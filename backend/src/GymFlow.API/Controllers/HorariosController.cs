using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Horarios;
using GymFlow.API.Authorization;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HorariosController : ControllerBase
{
    private readonly GetHorariosQuery _getHorariosQuery;
    private readonly GetHorarioByIdQuery _getHorarioByIdQuery;
    private readonly CreateHorarioCommand _createHorarioCommand;
    private readonly UpdateHorarioCommand _updateHorarioCommand;
    private readonly DeleteHorarioCommand _deleteHorarioCommand;

    public HorariosController(
        GetHorariosQuery getHorariosQuery,
        GetHorarioByIdQuery getHorarioByIdQuery,
        CreateHorarioCommand createHorarioCommand,
        UpdateHorarioCommand updateHorarioCommand,
        DeleteHorarioCommand deleteHorarioCommand)
    {
        _getHorariosQuery = getHorariosQuery;
        _getHorarioByIdQuery = getHorarioByIdQuery;
        _createHorarioCommand = createHorarioCommand;
        _updateHorarioCommand = updateHorarioCommand;
        _deleteHorarioCommand = deleteHorarioCommand;
    }

    [HttpGet]
    [RequierePermiso(Modulo.Clases, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<HorarioClaseDto>>> GetAll([FromQuery] Guid? unidadId)
    {
        var horarios = await _getHorariosQuery.ExecuteAsync(unidadId);
        return Ok(horarios);
    }

    [HttpGet("{id:guid}")]
    [RequierePermiso(Modulo.Clases, Operacion.Lectura)]
    public async Task<ActionResult<HorarioClaseDto>> GetById(Guid id)
    {
        try
        {
            var horario = await _getHorarioByIdQuery.ExecuteAsync(id);
            return Ok(horario);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    [RequierePermiso(Modulo.Clases, Operacion.Escritura)]
    public async Task<ActionResult<HorarioClaseDto>> Create([FromBody] CreateHorarioClaseRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var horario = await _createHorarioCommand.ExecuteAsync(request, userId, userName);
            return CreatedAtAction(nameof(GetById), new { id = horario.Id }, horario);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Modulo.Clases, Operacion.Modificacion)]
    public async Task<ActionResult<HorarioClaseDto>> Update(Guid id, [FromBody] UpdateHorarioClaseRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var horario = await _updateHorarioCommand.ExecuteAsync(id, request, userId, userName);
            return Ok(horario);
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
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _deleteHorarioCommand.ExecuteAsync(id, userId, userName);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
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
