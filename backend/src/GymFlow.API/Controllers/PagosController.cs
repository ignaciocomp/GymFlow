using System.Security.Claims;
using System.Text.Json.Serialization;
using GymFlow.Application.UseCases.Pagos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagosController : ControllerBase
{
    private readonly IniciarPagoCuotaCommand _iniciarPagoCuotaCommand;
    private readonly ProcesarWebhookPagoCommand _procesarWebhookPagoCommand;
    private readonly GetMisPagosQuery _getMisPagosQuery;
    private readonly ILogger<PagosController> _logger;

    public PagosController(
        IniciarPagoCuotaCommand iniciarPagoCuotaCommand,
        ProcesarWebhookPagoCommand procesarWebhookPagoCommand,
        GetMisPagosQuery getMisPagosQuery,
        ILogger<PagosController> logger)
    {
        _iniciarPagoCuotaCommand = iniciarPagoCuotaCommand;
        _procesarWebhookPagoCommand = procesarWebhookPagoCommand;
        _getMisPagosQuery = getMisPagosQuery;
        _logger = logger;
    }

    /// <summary>
    /// RF-23 / CA-33: el socio inicia el pago online de una de sus cuotas pendientes.
    /// Devuelve el init_point de Checkout Pro al que el frontend debe redirigir.
    /// </summary>
    [HttpPost("iniciar")]
    public async Task<IActionResult> Iniciar([FromBody] IniciarPagoRequest body)
    {
        var socioId = GetSocioId();
        try
        {
            var resultado = await _iniciarPagoCuotaCommand.ExecuteAsync(body.CuotaId, socioId);
            return Ok(new { initPoint = resultado.InitPoint });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    /// <summary>
    /// RF-23 — webhook de Mercado Pago (`[AllowAnonymous]`, RN-31). Acepta los DOS formatos
    /// en que MP notifica pagos:
    /// <list type="bullet">
    /// <item><b>Webhook moderno</b>: <c>?data.id=...&amp;type=payment</c> y/o body
    /// <c>{ type, data: { id } }</c>, firmado con HMAC (<c>x-signature</c>). La seguridad la da
    /// la validación de firma dentro del command; responde <b>401 solo si la firma es
    /// inválida</b> y 200 en el resto. Para la firma se usa el <c>data.id</c> del QUERY
    /// (es el valor que MP firma), con fallback al del body.</item>
    /// <item><b>IPN legacy</b> (evento "Pagos" del panel): <c>?topic=payment&amp;id=...</c>,
    /// SIN <c>data.id</c> ni firma validable (docs MP). Se procesa por el camino IPN del
    /// command, que omite la firma pero consulta el estado REAL en la API de MP con nuestro
    /// token — nunca se confía en la notificación. Siempre 200.</item>
    /// </list>
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] WebhookRequest? body)
    {
        // --- Formato moderno: data.id por query (valor firmado por MP) o body ---
        var queryDataId = Request.Query["data.id"].FirstOrDefault();
        var dataId = !string.IsNullOrWhiteSpace(queryDataId) ? queryDataId : body?.Data?.Id;

        if (!string.IsNullOrWhiteSpace(dataId))
        {
            var tipo = Request.Query["type"].FirstOrDefault()
                ?? body?.Type
                ?? Request.Query["topic"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tipo) && !string.Equals(tipo, "payment", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Webhook MP (moderno) ignorado: type={Tipo} no es \"payment\". data.id={DataId}.",
                    tipo, dataId);
                return StatusCode(StatusCodes.Status200OK);
            }

            var xSignature = Request.Headers["x-signature"].FirstOrDefault();
            var xRequestId = Request.Headers["x-request-id"].FirstOrDefault();

            var resultado = await _procesarWebhookPagoCommand.ExecuteAsync(dataId, xSignature, xRequestId);
            _logger.LogInformation(
                "Webhook MP (moderno): data.id={DataId} → {Resultado}.", dataId, resultado);

            return resultado == WebhookResultado.FirmaInvalida
                ? StatusCode(StatusCodes.Status401Unauthorized)
                : StatusCode(StatusCodes.Status200OK);
        }

        // --- Formato IPN legacy: ?topic=payment&id=123456 ---
        var topic = Request.Query["topic"].FirstOrDefault();
        var ipnId = Request.Query["id"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(topic) && !string.IsNullOrWhiteSpace(ipnId))
        {
            if (!string.Equals(topic, "payment", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Webhook MP (IPN) ignorado: topic={Topic} no es \"payment\". id={Id}.", topic, ipnId);
                return StatusCode(StatusCodes.Status200OK);
            }

            var resultadoIpn = await _procesarWebhookPagoCommand.ExecuteAsync(ipnId, null, null, esIpn: true);
            _logger.LogInformation(
                "Webhook MP (IPN): id={Id} → {Resultado}.", ipnId, resultadoIpn);

            // IPN siempre 200: sin firma que rechazar; un id desconocido queda Ignorado.
            return StatusCode(StatusCodes.Status200OK);
        }

        // Ningún formato reconocible: 200 para que MP no reintente, pero con rastro en el log.
        _logger.LogWarning(
            "Webhook MP sin formato reconocible (ni data.id ni topic/id). Query keys: [{QueryKeys}].",
            string.Join(", ", Request.Query.Keys));
        return StatusCode(StatusCodes.Status200OK);
    }

    /// <summary>
    /// RF-23 / CU-08 — historial de pagos del socio autenticado.
    /// </summary>
    [HttpGet("mis-pagos")]
    public async Task<ActionResult<IEnumerable<PagoDto>>> MisPagos()
    {
        var socioId = GetSocioId();
        var pagos = await _getMisPagosQuery.ExecuteAsync(socioId);
        return Ok(pagos);
    }

    private Guid GetSocioId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
}

public record IniciarPagoRequest(Guid CuotaId);

/// <summary>Payload del webhook de MP: <c>{ type?, data: { id } }</c>.</summary>
public class WebhookRequest
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public WebhookData? Data { get; set; }
}

public class WebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
