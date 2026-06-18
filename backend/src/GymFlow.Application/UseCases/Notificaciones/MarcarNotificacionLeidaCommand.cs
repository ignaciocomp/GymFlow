using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Notificaciones;

public class MarcarNotificacionLeidaCommand
{
    private readonly INotificacionRepository _repository;

    public MarcarNotificacionLeidaCommand(INotificacionRepository repository) => _repository = repository;

    public async Task ExecuteAsync(Guid notificacionId, Guid socioId)
    {
        var notificacion = await _repository.GetByIdAsync(notificacionId);

        // Ownership: si no existe o es de otro socio, devolvemos NotFound (no filtramos
        // la existencia de notificaciones ajenas).
        if (notificacion is null || notificacion.SocioId != socioId)
            throw new KeyNotFoundException("La notificación no existe.");

        notificacion.MarcarLeida(DateTime.UtcNow);
        await _repository.SaveChangesAsync();
    }
}
