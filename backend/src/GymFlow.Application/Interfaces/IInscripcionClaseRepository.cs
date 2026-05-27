using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IInscripcionClaseRepository
{
    Task<InscripcionClase?> GetByIdAsync(Guid id);
    Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId);
    Task<InscripcionClase?> GetActivaBySocioYClaseAsync(Guid socioId, Guid claseId);
    Task<int> GetInscripcionesActivasCountAsync(Guid claseId);
    Task AddAsync(InscripcionClase inscripcion);
    Task SaveChangesAsync();
}
