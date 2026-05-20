using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Clases;
using GymFlow.API.Authorization;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClasesController : ControllerBase
{
    private readonly GetClasesQuery _getClasesQuery;
    private readonly GetClaseByIdQuery _getClaseByIdQuery;
    private readonly CreateClaseCommand _createClaseCommand;
    private readonly UpdateClaseCommand _updateClaseCommand;
    private readonly CancelClaseCommand _cancelClaseCommand;
    private readonly ReactivarClaseCommand _reactivarClaseCommand;

    public ClasesController(
        GetClasesQuery getClasesQuery,
        GetClaseByIdQuery getClaseByIdQuery,
        CreateClaseCommand createClaseCommand,
        UpdateClaseCommand updateClaseCommand,
        CancelClaseCommand cancelClaseCommand,
        ReactivarClaseCommand reactivarClaseCommand)
    {
        _getClasesQuery = getClasesQuery;
        _getClaseByIdQuery = getClaseByIdQuery;
        _createClaseCommand = createClaseCommand;
        _updateClaseCommand = updateClaseCommand;
        _cancelClaseCommand = cancelClaseCommand;
        _reactivarClaseCommand = reactivarClaseCommand;
    }

    [HttpGet]
    [RequierePermiso(Modulo.Clases, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<ClaseDto>>> GetAll(
        [FromQuery] Guid? unidadId,
        [FromQuery] bool includeInactive = false)
    {
        var clases = await _getClasesQuery.ExecuteAsync(unidadId, includeInactive);
        return Ok(clases);
    }

    [HttpGet("{id:guid}")]
    [RequierePermiso(Modulo.Clases, Operacion.Lectura)]
    public async Task<ActionResult<ClaseDto>> GetById(Guid id)
    {
        try
        {
            var clase = await _getClaseByIdQuery.ExecuteAsync(id);
            return Ok(clase);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    [RequierePermiso(Modulo.Clases, Operacion.Escritura)]
    public async Task<ActionResult<ClaseDto>> Create([FromBody] CreateClaseRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var clase = await _createClaseCommand.ExecuteAsync(request, userId, userName);
            return CreatedAtAction(nameof(GetById), new { id = clase.Id }, clase);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Modulo.Clases, Operacion.Modificacion)]
    public async Task<ActionResult<ClaseDto>> Update(Guid id, [FromBody] UpdateClaseRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var clase = await _updateClaseCommand.ExecuteAsync(id, request, userId, userName);
            return Ok(clase);
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

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Clases, Operacion.Eliminacion)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _cancelClaseCommand.ExecuteAsync(id, userId, userName);
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

    [HttpPatch("{id:guid}/reactivar")]
    [RequierePermiso(Modulo.Clases, Operacion.Modificacion)]
    public async Task<ActionResult<ClaseDto>> Reactivar(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var clase = await _reactivarClaseCommand.ExecuteAsync(id, userId, userName);
            return Ok(clase);
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

    private (Guid Id, string Nombre) GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var nombre = User.FindFirst("nombre")?.Value ?? "";
        var apellido = User.FindFirst("apellido")?.Value ?? "";
        var fullName = $"{nombre} {apellido}".Trim();
        return (userId, string.IsNullOrWhiteSpace(fullName) ? "Sistema" : fullName);
    }
}
