using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.API.Authorization;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosController : ControllerBase
{
    private readonly GetEventosQuery _getEventosQuery;
    private readonly GetEventoByIdQuery _getEventoByIdQuery;
    private readonly CrearEventoCommand _crearEventoCommand;
    private readonly ActualizarEventoCommand _actualizarEventoCommand;
    private readonly CancelarEventoCommand _cancelarEventoCommand;
    private readonly NotificarEventoCommand _notificarEventoCommand;
    private readonly IUnidadesVisiblesResolver _unidadesResolver;

    public EventosController(
        GetEventosQuery getEventosQuery,
        GetEventoByIdQuery getEventoByIdQuery,
        CrearEventoCommand crearEventoCommand,
        ActualizarEventoCommand actualizarEventoCommand,
        CancelarEventoCommand cancelarEventoCommand,
        NotificarEventoCommand notificarEventoCommand,
        IUnidadesVisiblesResolver unidadesResolver)
    {
        _getEventosQuery = getEventosQuery;
        _getEventoByIdQuery = getEventoByIdQuery;
        _crearEventoCommand = crearEventoCommand;
        _actualizarEventoCommand = actualizarEventoCommand;
        _cancelarEventoCommand = cancelarEventoCommand;
        _notificarEventoCommand = notificarEventoCommand;
        _unidadesResolver = unidadesResolver;
    }

    [HttpGet]
    [RequierePermiso(Modulo.Eventos, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<EventoDto>>> GetAll(
        [FromQuery] Guid? unidadId,
        [FromQuery] bool incluirInactivos = false)
    {
        var (userId, rolId) = GetCurrentActor();
        var unidadesPermitidas = await _unidadesResolver.ResolverAsync(userId, rolId);
        var eventos = await _getEventosQuery.ExecuteAsync(unidadId, incluirInactivos, unidadesPermitidas);
        return Ok(eventos);
    }

    // Identidad del actuante (userId + rolId) del JWT, para resolver server-side las unidades visibles.
    private (Guid UserId, Guid RolId) GetCurrentActor()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var rolId = Guid.TryParse(User.FindFirst("rolId")?.Value, out var r) ? r : Guid.Empty;
        return (userId, rolId);
    }

    [HttpGet("{id:guid}")]
    [RequierePermiso(Modulo.Eventos, Operacion.Lectura)]
    public async Task<ActionResult<EventoDto>> GetById(Guid id)
    {
        try
        {
            var evento = await _getEventoByIdQuery.ExecuteAsync(id);
            return Ok(evento);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    [RequierePermiso(Modulo.Eventos, Operacion.Escritura)]
    public async Task<ActionResult<EventoDto>> Create([FromBody] CreateEventoRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var evento = await _crearEventoCommand.ExecuteAsync(request, userId, userName);
            return CreatedAtAction(nameof(GetById), new { id = evento.Id }, evento);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Modulo.Eventos, Operacion.Modificacion)]
    public async Task<ActionResult<EventoDto>> Update(Guid id, [FromBody] UpdateEventoRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var evento = await _actualizarEventoCommand.ExecuteAsync(id, request, userId, userName);
            return Ok(evento);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Eventos, Operacion.Eliminacion)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _cancelarEventoCommand.ExecuteAsync(id, userId, userName);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/notificar")]
    [RequierePermiso(Modulo.Eventos, Operacion.Escritura)]
    public async Task<IActionResult> Notificar(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _notificarEventoCommand.ExecuteAsync(id, userId, userName);
            return Ok(new { mensaje = "Se reenviaron las notificaciones a los socios de la sede." });
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
