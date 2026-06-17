using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Eventos;

public class GetEventoByIdQuery
{
    private readonly IEventoRepository _repository;

    public GetEventoByIdQuery(IEventoRepository repository)
    {
        _repository = repository;
    }

    public async Task<EventoDto> ExecuteAsync(Guid id)
    {
        var evento = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El evento no fue encontrado.");

        return EventoMapper.ToDto(evento);
    }
}
