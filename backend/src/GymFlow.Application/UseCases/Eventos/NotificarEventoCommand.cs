using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Eventos;

/// <summary>
/// Reenvía la notificación de un evento a los socios activos de su unidad.
/// Reusa el helper compartido <see cref="EventoNotificador"/> (envío best-effort,
/// mismo patrón que <c>CancelClaseCommand</c>).
/// </summary>
public class NotificarEventoCommand
{
    private readonly IEventoRepository _eventoRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;

    public NotificarEventoCommand(
        IEventoRepository eventoRepository,
        ISocioRepository socioRepository,
        IAuditLogger auditLogger,
        IEmailService emailService)
    {
        _eventoRepository = eventoRepository;
        _socioRepository = socioRepository;
        _auditLogger = auditLogger;
        _emailService = emailService;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var evento = await _eventoRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El evento no fue encontrado.");

        var socios = await _socioRepository.GetActivosByUnidadAsync(evento.UnidadId);
        var resultado = await EventoNotificador.NotificarAsync(_emailService, socios, evento);

        var detalle = resultado.Fallidos > 0
            ? $"Se reenviaron notificaciones del evento '{evento.Titulo}'. Se notificaron {resultado.Enviados} de {resultado.Total} socios ({resultado.Fallidos} envíos fallaron)."
            : $"Se reenviaron notificaciones del evento '{evento.Titulo}'. Se notificaron {resultado.Enviados} socios.";

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Evento", evento.Id,
            detalle);
    }
}
