using System.Security.Claims;
using System.Text.Json;
using GymFlow.API.Authorization;
using GymFlow.API.Sse;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Dashboard;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GymFlow.API.Controllers;

/// <summary>
/// RF-18 / CU-10: dashboard operativo de solo lectura. Snapshot para la carga inicial y el
/// polling de fallback, y stream SSE que recalcula cada ~10s y emite solo si cambió
/// (cumple la antigüedad máxima de 30s de RN-15 sin recargar, RNF-02).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private static readonly TimeSpan IntervaloStream = TimeSpan.FromSeconds(10);

    private readonly GetDashboardQuery _getDashboardQuery;
    private readonly IUnidadesVisiblesResolver _unidadesResolver;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public DashboardController(
        GetDashboardQuery getDashboardQuery,
        IUnidadesVisiblesResolver unidadesResolver,
        IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions)
    {
        _getDashboardQuery = getDashboardQuery;
        _unidadesResolver = unidadesResolver;
        // Opciones JSON de MVC: el stream serializa en camelCase igual que el snapshot.
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    [HttpGet]
    [RequierePermiso(Modulo.Dashboard, Operacion.Lectura)]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] Guid? unidadId)
    {
        var (userId, rolId) = GetCurrentActor();
        var unidadesPermitidas = await _unidadesResolver.ResolverAsync(userId, rolId, HttpContext.RequestAborted);

        try
        {
            var dashboard = await _getDashboardQuery.ExecuteAsync(unidadId, unidadesPermitidas);
            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// SSE: cada ~10s recalcula el snapshot; si difiere del último enviado emite
    /// <c>data: {json}</c>, sino un comentario heartbeat <c>: ping</c>. El primer snapshot
    /// se emite siempre. El permiso y la unidad pedida se validan ANTES de iniciar el
    /// stream, cuando todavía se puede responder 403.
    /// </summary>
    [HttpGet("stream")]
    [RequierePermiso(Modulo.Dashboard, Operacion.Lectura)]
    public async Task<IActionResult> Stream([FromQuery] Guid? unidadId)
    {
        var ct = HttpContext.RequestAborted;
        var (userId, rolId) = GetCurrentActor();
        var unidadesPermitidas = await _unidadesResolver.ResolverAsync(userId, rolId, ct);

        DashboardDto snapshot;
        try
        {
            snapshot = await _getDashboardQuery.ExecuteAsync(unidadId, unidadesPermitidas);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // sin buffering en proxies (nginx)

        string? ultimoEnviado = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var json = JsonSerializer.Serialize(snapshot, _jsonSerializerOptions);

                if (DashboardSnapshotDiff.HaCambiado(ultimoEnviado, json))
                {
                    await Response.WriteAsync($"data: {json}\n\n", ct);
                    ultimoEnviado = json;
                }
                else
                {
                    await Response.WriteAsync(": ping\n\n", ct);
                }

                await Response.Body.FlushAsync(ct);
                await Task.Delay(IntervaloStream, ct);

                snapshot = await _getDashboardQuery.ExecuteAsync(unidadId, unidadesPermitidas);
            }
        }
        catch (OperationCanceledException)
        {
            // Cliente desconectado (cerró la pestaña): Task.Delay/WriteAsync lanzan con el
            // RequestAborted y la respuesta ya está iniciada — es el cierre normal del stream.
        }

        return new EmptyResult();
    }

    // Identidad del actuante (userId + rolId) del JWT, para resolver server-side las unidades visibles.
    private (Guid UserId, Guid RolId) GetCurrentActor()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var rolId = Guid.TryParse(User.FindFirst("rolId")?.Value, out var r) ? r : Guid.Empty;
        return (userId, rolId);
    }
}
