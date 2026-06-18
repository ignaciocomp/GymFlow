using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Notificaciones;

public class ContarNoLeidasQuery
{
    private readonly INotificacionRepository _repository;

    public ContarNoLeidasQuery(INotificacionRepository repository) => _repository = repository;

    public Task<int> ExecuteAsync(Guid socioId) => _repository.ContarNoLeidasAsync(socioId);
}
