using System.Security.Claims;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Inscripciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

// Self-service del socio: este controller usa solo [Authorize] y no [RequierePermiso]
// porque el socio opera exclusivamente sobre sus propios datos (sus inscripciones).
// Los commands validan la ownership del recurso (que la inscripcion/clase pertenezca
// al socio autenticado), por lo que no se requiere un permiso de modulo administrativo.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InscripcionesController : ControllerBase
{
    private readonly InscribirSocioCommand _inscribirCommand;
    private readonly CancelarInscripcionCommand _cancelarCommand;
    private readonly GetMisInscripcionesQuery _misInscripcionesQuery;

    public InscripcionesController(
        InscribirSocioCommand inscribirCommand,
        CancelarInscripcionCommand cancelarCommand,
        GetMisInscripcionesQuery misInscripcionesQuery)
    {
        _inscribirCommand = inscribirCommand;
        _cancelarCommand = cancelarCommand;
        _misInscripcionesQuery = misInscripcionesQuery;
    }

    [HttpPost]
    public async Task<ActionResult<InscripcionClaseDto>> Inscribirse([FromBody] InscribirSocioRequest request)
    {
        try
        {
            var socioId = GetSocioId();
            var (usuarioId, usuarioNombre) = GetCurrentUser();
            // El socio actua sobre si mismo: socioId y usuarioId son el mismo valor,
            // pero se pasan a ambos parametros para la auditoria del command.
            var inscripcion = await _inscribirCommand.ExecuteAsync(socioId, request.HorarioClaseId, usuarioId, usuarioNombre);
            return Ok(inscripcion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("mis-inscripciones")]
    public async Task<ActionResult<IEnumerable<InscripcionClaseDto>>> GetMisInscripciones()
    {
        var socioId = GetSocioId();
        var inscripciones = await _misInscripcionesQuery.ExecuteAsync(socioId);
        return Ok(inscripciones);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelarInscripcion(Guid id)
    {
        try
        {
            var socioId = GetSocioId();
            var (usuarioId, usuarioNombre) = GetCurrentUser();
            await _cancelarCommand.ExecuteAsync(id, socioId, usuarioId, usuarioNombre);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private Guid GetSocioId()
    {
        return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    }

    private (Guid Id, string Nombre) GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var nombre = User.FindFirst("nombre")?.Value ?? "";
        var apellido = User.FindFirst("apellido")?.Value ?? "";
        var fullName = $"{nombre} {apellido}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = User.FindFirst(ClaimTypes.Email)?.Value ?? "Socio";
        }
        return (userId, fullName);
    }
}
