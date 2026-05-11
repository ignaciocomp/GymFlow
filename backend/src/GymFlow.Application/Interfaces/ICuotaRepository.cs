using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface ICuotaRepository
{
    Task<Cuota?> GetByIdAsync(Guid id);
    Task<IEnumerable<Cuota>> GetBySocioIdAsync(Guid socioId);
    Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false);
    Task<Cuota?> GetUltimaCuotaAsync(Guid socioId, Guid unidadId);
    Task AddAsync(Cuota cuota);
    Task DeletePendientesBySocioAsync(Guid socioId);
    Task SaveChangesAsync();
}
