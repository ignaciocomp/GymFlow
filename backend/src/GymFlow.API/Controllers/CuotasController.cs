using System.Security.Claims;
using GymFlow.API.Authorization;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CuotasController : ControllerBase
{
    private readonly GetCuotasBySocioQuery _getCuotasBySocioQuery;
    private readonly GetCuotasAdminQuery _getCuotasAdminQuery;
    private readonly MarcarCuotaPagadaCommand _marcarPagadaCommand;
    private readonly AnularCuotaCommand _anularCommand;
    private readonly RevertirPagoCuotaCommand _revertirPagoCommand;
    private readonly RevertirAnulacionCuotaCommand _revertirAnulacionCommand;
    private readonly NotificarCuotaCommand _notificarCommand;
    private readonly GetSociosConEstadoCuotaQuery _getSociosConEstadoCuotaQuery;
    private readonly IUnidadesVisiblesResolver _unidadesResolver;

    public CuotasController(
        GetCuotasBySocioQuery getCuotasBySocioQuery,
        GetCuotasAdminQuery getCuotasAdminQuery,
        MarcarCuotaPagadaCommand marcarPagadaCommand,
        AnularCuotaCommand anularCommand,
        RevertirPagoCuotaCommand revertirPagoCommand,
        RevertirAnulacionCuotaCommand revertirAnulacionCommand,
        NotificarCuotaCommand notificarCommand,
        GetSociosConEstadoCuotaQuery getSociosConEstadoCuotaQuery,
        IUnidadesVisiblesResolver unidadesResolver)
    {
        _getCuotasBySocioQuery = getCuotasBySocioQuery;
        _getCuotasAdminQuery = getCuotasAdminQuery;
        _marcarPagadaCommand = marcarPagadaCommand;
        _anularCommand = anularCommand;
        _revertirPagoCommand = revertirPagoCommand;
        _revertirAnulacionCommand = revertirAnulacionCommand;
        _notificarCommand = notificarCommand;
        _getSociosConEstadoCuotaQuery = getSociosConEstadoCuotaQuery;
        _unidadesResolver = unidadesResolver;
    }

    [HttpGet("mis-cuotas")]
    public async Task<ActionResult<IEnumerable<CuotaDto>>> GetMisCuotas()
    {
        var socioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var cuotas = await _getCuotasBySocioQuery.ExecuteAsync(socioId);
        return Ok(cuotas);
    }

    [HttpGet("admin")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<CuotaDto>>> GetAdmin(
        [FromQuery] string documentoIdentidad,
        [FromQuery] EstadoCuota? estado,
        [FromQuery] int? mes,
        [FromQuery] int? anio,
        [FromQuery] Guid? unidadId,
        [FromQuery] bool incluirAnuladas = false)
    {
        try
        {
            var (userId, rolId) = GetCurrentActor();
            var unidadesPermitidas = await _unidadesResolver.ResolverAsync(userId, rolId);
            var cuotas = await _getCuotasAdminQuery.ExecuteAsync(documentoIdentidad, estado, mes, anio, unidadId, incluirAnuladas, unidadesPermitidas);
            return Ok(cuotas);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// RF-07: lista todos los socios con un resumen del estado de sus cuotas.
    /// Usado por la vista admin de "Gestión de cuotas" para mostrar socios filtrados por unidad.
    /// </summary>
    [HttpGet("socios-estado")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<SocioConEstadoCuotaDto>>> GetSociosEstado(
        [FromQuery] Guid? unidadId)
    {
        var (userId, rolId) = GetCurrentActor();
        var unidadesPermitidas = await _unidadesResolver.ResolverAsync(userId, rolId);
        var socios = await _getSociosConEstadoCuotaQuery.ExecuteAsync(unidadId, unidadesPermitidas);
        return Ok(socios);
    }

    /// <summary>
    /// RF-07: obtiene las cuotas de un socio específico por su ID (usado por la vista detalle).
    /// </summary>
    [HttpGet("admin/socio/{socioId:guid}")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<CuotaDto>>> GetCuotasBySocioId(
        Guid socioId,
        [FromQuery] EstadoCuota? estado,
        [FromQuery] int? mes,
        [FromQuery] int? anio,
        [FromQuery] Guid? unidadId,
        [FromQuery] bool incluirAnuladas = false)
    {
        try
        {
            var (userId, rolId) = GetCurrentActor();
            var unidadesPermitidas = await _unidadesResolver.ResolverAsync(userId, rolId);
            var cuotas = await _getCuotasAdminQuery.ExecuteBySocioIdAsync(socioId, estado, mes, anio, unidadId, incluirAnuladas, unidadesPermitidas);
            return Ok(cuotas);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/pagar")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Modificacion)]
    public async Task<ActionResult<CuotaDto>> MarcarComoPagada(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var dto = await _marcarPagadaCommand.ExecuteAsync(id, userId, userName);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Eliminacion)]
    public async Task<ActionResult<CuotaDto>> Anular(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var dto = await _anularCommand.ExecuteAsync(id, userId, userName);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/revertir-pago")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Modificacion)]
    public async Task<ActionResult<CuotaDto>> RevertirPago(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var dto = await _revertirPagoCommand.ExecuteAsync(id, userId, userName);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/notificar")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Modificacion)]
    public async Task<IActionResult> Notificar(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _notificarCommand.ExecuteAsync(id, userId, userName);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/revertir-anulacion")]
    [RequierePermiso(Modulo.Cuotas, Operacion.Modificacion)]
    public async Task<ActionResult<CuotaDto>> RevertirAnulacion(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var dto = await _revertirAnulacionCommand.ExecuteAsync(id, userId, userName);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    private (Guid Id, string Nombre) GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var nombre = User.FindFirst("nombre")?.Value ?? "";
        var apellido = User.FindFirst("apellido")?.Value ?? "";
        var fullName = $"{nombre} {apellido}".Trim();
        return (userId, string.IsNullOrWhiteSpace(fullName) ? "Sistema" : fullName);
    }

    // Identidad del actuante (userId + rolId) tomada del JWT, para resolver server-side
    // las unidades visibles. Nunca se confía en parámetros de la request para el scoping.
    private (Guid UserId, Guid RolId) GetCurrentActor()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var rolId = Guid.TryParse(User.FindFirst("rolId")?.Value, out var r) ? r : Guid.Empty;
        return (userId, rolId);
    }
}
