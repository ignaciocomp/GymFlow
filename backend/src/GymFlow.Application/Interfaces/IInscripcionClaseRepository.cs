using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IInscripcionClaseRepository
{
    Task<InscripcionClase?> GetByIdAsync(Guid id);
    Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId);
    Task<InscripcionClase?> GetActivaBySocioYClaseAsync(Guid socioId, Guid claseId);
    Task<int> GetInscripcionesActivasCountAsync(Guid claseId);
    Task<InscripcionClase?> GetPrimeroEnListaEsperaAsync(Guid claseId);
    Task<int> GetPosicionEnListaEsperaAsync(Guid inscripcionId);
    Task<System.Collections.Generic.Dictionary<Guid, int>> GetConteoActivasPorClasesAsync(System.Collections.Generic.IEnumerable<Guid> claseIds);
    Task AddAsync(InscripcionClase inscripcion);
    Task SaveChangesAsync();
}
