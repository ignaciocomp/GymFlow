using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Socios;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SociosController : ControllerBase
{
    private readonly CreateSocioCommand _createSocioCommand;
    private readonly GetSociosQuery _getSociosQuery;
    private readonly DeleteSocioCommand _deleteSocioCommand;
    private readonly GetSocioByIdQuery _getSocioByIdQuery;
    private readonly UpdateSocioCommand _updateSocioCommand;
    private readonly ReactivateSocioCommand _reactivateSocioCommand;

    public SociosController(
        CreateSocioCommand createSocioCommand,
        GetSociosQuery getSociosQuery,
        DeleteSocioCommand deleteSocioCommand,
        GetSocioByIdQuery getSocioByIdQuery,
        UpdateSocioCommand updateSocioCommand,
        ReactivateSocioCommand reactivateSocioCommand)
    {
        _createSocioCommand = createSocioCommand;
        _getSociosQuery = getSociosQuery;
        _deleteSocioCommand = deleteSocioCommand;
        _getSocioByIdQuery = getSocioByIdQuery;
        _updateSocioCommand = updateSocioCommand;
        _reactivateSocioCommand = reactivateSocioCommand;
    }

    /// <summary>
    /// RF-02: List socios with search and filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SocioDto>>> GetAll(
        [FromQuery] string? nombre,
        [FromQuery] Guid? unidadId,
        [FromQuery] Guid? planId,
        [FromQuery] bool? estaActivo)
    {
        var socios = await _getSociosQuery.ExecuteAsync(nombre, unidadId, planId, estaActivo);
        return Ok(socios);
    }

    /// <summary>
    /// Get a socio by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SocioDto>> GetById(Guid id)
    {
        try
        {
            var socio = await _getSocioByIdQuery.ExecuteAsync(id);
            return Ok(socio);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// RF-01: Register a new socio
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SocioDto>> Create([FromBody] CreateSocioRequest request)
    {
        try
        {
            var socio = await _createSocioCommand.ExecuteAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = socio.Id }, socio);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// RF-03: Update socio data
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SocioDto>> Update(Guid id, [FromBody] UpdateSocioRequest request)
    {
        try
        {
            var socio = await _updateSocioCommand.ExecuteAsync(id, request);
            return Ok(socio);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reactivate a soft-deleted socio
    /// </summary>
    [HttpPatch("{id:guid}/reactivar")]
    public async Task<ActionResult<SocioDto>> Reactivar(Guid id)
    {
        try
        {
            var socio = await _reactivateSocioCommand.ExecuteAsync(id);
            return Ok(socio);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// RF-04: Soft delete (baja lógica) a socio
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteSocioRequest? request)
    {
        try
        {
            await _deleteSocioCommand.ExecuteAsync(id, request?.Motivo);
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
}
