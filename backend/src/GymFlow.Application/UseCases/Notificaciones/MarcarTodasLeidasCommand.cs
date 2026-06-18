using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Notificaciones;

public class MarcarTodasLeidasCommand
{
    private readonly INotificacionRepository _repository;

    public MarcarTodasLeidasCommand(INotificacionRepository repository) => _repository = repository;

    public async Task ExecuteAsync(Guid socioId)
    {
        await _repository.MarcarTodasLeidasAsync(socioId, DateTime.UtcNow);
        await _repository.SaveChangesAsync();
    }
}
