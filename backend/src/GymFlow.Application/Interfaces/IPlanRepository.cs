using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IPlanRepository
{
    Task<IEnumerable<Plan>> GetAllAsync(bool includeInactive = false);
    Task<Plan?> GetByIdAsync(Guid id);
    Task<IEnumerable<Plan>> GetByUnidadIdAsync(Guid unidadId);
    Task<bool> ExisteSocioConPlanAsync(Guid planId);
    Task AddAsync(Plan plan);
    Task SaveChangesAsync();
}
