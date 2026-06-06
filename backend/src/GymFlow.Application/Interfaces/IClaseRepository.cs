using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IClaseRepository
{
    Task<IEnumerable<Clase>> GetAllAsync(bool includeInactive = false);
    Task<Clase?> GetByIdAsync(Guid id);
    Task<IEnumerable<Clase>> GetByUnidadIdAsync(Guid unidadId, bool includeInactive = false);
    Task AddAsync(Clase clase);
    Task SaveChangesAsync();
}
