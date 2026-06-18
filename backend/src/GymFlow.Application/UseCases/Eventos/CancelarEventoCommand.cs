using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Eventos;

public class CancelarEventoCommand
{
    private readonly IEventoRepository _eventoRepository;
    private readonly IAuditLogger _auditLogger;

    public CancelarEventoCommand(
        IEventoRepository eventoRepository,
        IAuditLogger auditLogger)
    {
        _eventoRepository = eventoRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var evento = await _eventoRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El evento no fue encontrado.");

        // Baja lógica idempotente (no notifica: la notificación es un comando aparte).
        evento.Cancelar();

        await _eventoRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Evento", evento.Id,
            $"Se canceló el evento '{evento.Titulo}'.");
    }
}
