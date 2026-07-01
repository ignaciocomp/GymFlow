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

    public PagosController(
        IniciarPagoCuotaCommand iniciarPagoCuotaCommand,
        ProcesarWebhookPagoCommand procesarWebhookPagoCommand,
        GetMisPagosQuery getMisPagosQuery)
    {
        _iniciarPagoCuotaCommand = iniciarPagoCuotaCommand;
        _procesarWebhookPagoCommand = procesarWebhookPagoCommand;
        _getMisPagosQuery = getMisPagosQuery;
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
    /// RF-23 — webhook de Mercado Pago (`[AllowAnonymous]`, RN-31). La seguridad la da la
    /// validación de firma HMAC dentro del command, no la autenticación.
    /// Responde <b>401 solo si la firma es inválida</b> (spoofing — da igual que MP reintente)
    /// y <b>200 en el resto de los casos</b> (procesado / pago desconocido / pendiente) para
    /// que MP deje de reintentar. El <c>data.id</c> llega por query (<c>?data.id=</c>) o en el
    /// body (<c>{ data: { id } }</c> o <c>{ type, data: { id } }</c>).
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] WebhookRequest? body)
    {
        var dataId = body?.Data?.Id;
        if (string.IsNullOrWhiteSpace(dataId))
            dataId = Request.Query["data.id"].FirstOrDefault();

        // Sin data.id no hay nada que reconciliar: 200 para que MP no reintente.
        if (string.IsNullOrWhiteSpace(dataId))
            return StatusCode(StatusCodes.Status200OK);

        var xSignature = Request.Headers["x-signature"].FirstOrDefault();
        var xRequestId = Request.Headers["x-request-id"].FirstOrDefault();

        var resultado = await _procesarWebhookPagoCommand.ExecuteAsync(dataId, xSignature, xRequestId);

        return resultado == WebhookResultado.FirmaInvalida
            ? StatusCode(StatusCodes.Status401Unauthorized)
            : StatusCode(StatusCodes.Status200OK);
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
