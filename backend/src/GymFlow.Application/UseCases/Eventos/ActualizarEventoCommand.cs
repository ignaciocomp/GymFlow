using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Eventos;

public class ActualizarEventoCommand
{
    private readonly IEventoRepository _eventoRepository;
    private readonly IAuditLogger _auditLogger;

    public ActualizarEventoCommand(
        IEventoRepository eventoRepository,
        IAuditLogger auditLogger)
    {
        _eventoRepository = eventoRepository;
        _auditLogger = auditLogger;
    }

    public async Task<EventoDto> ExecuteAsync(Guid id, UpdateEventoRequest request, Guid usuarioId, string usuarioNombre)
    {
        var evento = await _eventoRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El evento no fue encontrado.");

        // Normalizar a UTC (Npgsql timestamptz exige Kind=Utc).
        // El dominio permite fecha pasada al actualizar: se respeta acá a propósito.
        var fechaUtc = DateTime.SpecifyKind(request.Fecha.ToUniversalTime(), DateTimeKind.Utc);

        // El método de dominio valida el título no vacío (ArgumentException).
        evento.Actualizar(request.Titulo, request.Descripcion ?? "", fechaUtc);

        await _eventoRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Evento", evento.Id,
            $"Se actualizó el evento '{evento.Titulo}'.");

        return EventoMapper.ToDto(evento);
    }
}
