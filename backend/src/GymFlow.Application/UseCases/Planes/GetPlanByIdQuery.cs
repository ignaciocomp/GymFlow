using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Planes;

public class GetPlanByIdQuery
{
    private readonly IPlanRepository _repository;

    public GetPlanByIdQuery(IPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlanDto> ExecuteAsync(Guid id)
    {
        var plan = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el plan con ID {id}.");

        return new PlanDto(plan.Id, plan.Nombre, plan.Precio, plan.Descripcion, plan.UnidadId, plan.Unidad?.Nombre ?? "", plan.EstaActivo);
    }
}
