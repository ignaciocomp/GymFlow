using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface ICuotaRepository
{
    Task<Cuota?> GetByIdAsync(Guid id);
    Task<IEnumerable<Cuota>> GetBySocioIdAsync(Guid socioId);
    Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false);
    Task<Cuota?> GetUltimaCuotaAsync(Guid socioId, Guid unidadId);
    /// <summary>True si el socio tiene al menos una cuota Pendiente y vencida en esa unidad.</summary>
    Task<bool> TieneCuotasVencidasEnUnidadAsync(Guid socioId, Guid unidadId);
    Task<IEnumerable<Cuota>> GetCuotasParaRecordatorioAsync(DateTime hoy);
    /// <summary>
    /// Devuelve todas las cuotas pendientes (no anuladas) de TODOS los socios en una sola query.
    /// Usado para calcular el estado general de cuota de muchos socios sin caer en N+1.
    /// </summary>
    Task<IEnumerable<Cuota>> GetCuotasPendientesDeTodosLosSociosAsync(Guid? unidadId = null);
    Task AddAsync(Cuota cuota);
    Task DeletePendientesBySocioAsync(Guid socioId);
    Task SaveChangesAsync();
}
