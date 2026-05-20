using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IClaseRepository
{
    Task<IEnumerable<Clase>> GetAllAsync(bool includeInactive = false);
    Task<Clase?> GetByIdAsync(Guid id);
    Task<IEnumerable<Clase>> GetByUnidadIdAsync(Guid unidadId, bool includeInactive = false);
    Task<int> GetInscripcionesActivasCountAsync(Guid claseId);
    Task<IEnumerable<InscripcionClase>> GetInscripcionesActivasAsync(Guid claseId);
    Task AddAsync(Clase clase);
    Task SaveChangesAsync();
}
