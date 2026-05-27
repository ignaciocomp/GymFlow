using System.Security.Claims;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Inscripciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

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
            var inscripcion = await _inscribirCommand.ExecuteAsync(socioId, request.ClaseId);
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
            await _cancelarCommand.ExecuteAsync(id, socioId);
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
}
