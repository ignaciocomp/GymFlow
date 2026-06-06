using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IInscripcionClaseRepository
{
    Task<InscripcionClase?> GetByIdAsync(Guid id);
    Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId);
    Task<InscripcionClase?> GetActivaBySocioYHorarioAsync(Guid socioId, Guid horarioClaseId);
    Task<int> GetInscripcionesActivasCountAsync(Guid horarioClaseId);
    Task<Dictionary<Guid, int>> GetConteoActivasPorHorariosAsync(IEnumerable<Guid> horarioClaseIds);
    Task<IEnumerable<InscripcionClase>> GetActivasByHorarioClaseIdAsync(Guid horarioClaseId);
    Task AddAsync(InscripcionClase inscripcion);
    Task SaveChangesAsync();
}
