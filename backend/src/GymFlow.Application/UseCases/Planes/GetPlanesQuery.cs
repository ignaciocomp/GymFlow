using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Planes;

public class GetPlanesQuery
{
    private readonly IPlanRepository _repository;

    public GetPlanesQuery(IPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PlanDto>> ExecuteAsync(Guid? unidadId = null, bool includeInactive = false)
    {
        var planes = unidadId.HasValue
            ? await _repository.GetByUnidadIdAsync(unidadId.Value)
            : await _repository.GetAllAsync(includeInactive);

        return planes.Select(p => new PlanDto(p.Id, p.Nombre, p.Precio, p.Descripcion, p.UnidadId, p.Unidad?.Nombre ?? "", p.EstaActivo));
    }
}
