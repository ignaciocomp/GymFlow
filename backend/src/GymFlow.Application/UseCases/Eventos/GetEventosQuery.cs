using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Eventos;

public class GetEventosQuery
{
    private readonly IEventoRepository _repository;

    public GetEventosQuery(IEventoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<EventoDto>> ExecuteAsync(Guid? unidadId = null, bool incluirInactivos = false, IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        var eventos = await _repository.GetAllAsync(unidadId, incluirInactivos, unidadesPermitidas);
        return eventos.Select(EventoMapper.ToDto);
    }
}
