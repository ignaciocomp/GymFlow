using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Unidades;

public class GetUnidadesQuery
{
    private readonly IUnidadRepository _repository;

    public GetUnidadesQuery(IUnidadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<UnidadDto>> ExecuteAsync()
    {
        var unidades = await _repository.GetAllAsync();
        return unidades.Select(u => new UnidadDto(u.Id, u.Nombre, u.Direccion));
    }
}
