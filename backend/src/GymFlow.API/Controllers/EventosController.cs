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
    private readonly ISocioRepository _socioRepository;

    public EventosController(
        GetEventosQuery getEventosQuery,
        GetEventoByIdQuery getEventoByIdQuery,
        CrearEventoCommand crearEventoCommand,
        ActualizarEventoCommand actualizarEventoCommand,
        CancelarEventoCommand cancelarEventoCommand,
        NotificarEventoCommand notificarEventoCommand,
        IUnidadesVisiblesResolver unidadesResolver,
        ISocioRepository socioRepository)
    {
        _getEventosQuery = getEventosQuery;
        _getEventoByIdQuery = getEventoByIdQuery;
        _crearEventoCommand = crearEventoCommand;
        _actualizarEventoCommand = actualizarEventoCommand;
        _cancelarEventoCommand = cancelarEventoCommand;
        _notificarEventoCommand = notificarEventoCommand;
        _unidadesResolver = unidadesResolver;
        _socioRepository = socioRepository;
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

    [HttpGet("{id:guid}/destinatarios")]
    [RequierePermiso(Modulo.Eventos, Operacion.Lectura)]
    public async Task<IActionResult> GetDestinatarios(Guid id)
    {
        try
        {
            var evento = await _getEventoByIdQuery.ExecuteAsync(id);
            var cantidad = await _socioRepository.CountActivosByUnidadAsync(evento.UnidadId);
            return Ok(new { cantidad, sede = evento.UnidadNombre });
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
            var resultado = await _notificarEventoCommand.ExecuteAsync(id, userId, userName);
            var mensaje = ConstruirMensajeNotificacion(resultado);
            return Ok(new { mensaje });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // Mensaje para el banner de la UI: cuántos socios se notificaron y de qué sede.
    private static string ConstruirMensajeNotificacion(NotificarEventoResultado resultado)
    {
        var sede = string.IsNullOrWhiteSpace(resultado.SedeNombre) ? "la sede" : resultado.SedeNombre;

        if (resultado.Total == 0)
            return $"No hay socios activos en {sede} para notificar.";

        var socios = resultado.Enviados == 1 ? "socio" : "socios";

        return resultado.Fallidos > 0
            ? $"Se notificó a {resultado.Enviados} de {resultado.Total} {socios} de {sede} ({resultado.Fallidos} envíos fallaron)."
            : $"Se notificó a {resultado.Enviados} {socios} de {sede}.";
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
