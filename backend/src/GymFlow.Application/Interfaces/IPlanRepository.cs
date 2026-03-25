using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IPlanRepository
{
    Task<IEnumerable<Plan>> GetAllAsync();
    Task<Plan?> GetByIdAsync(Guid id);
    Task<IEnumerable<Plan>> GetByUnidadIdAsync(Guid unidadId);
}
