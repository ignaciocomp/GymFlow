using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface ICuotaRepository
{
    Task<Cuota?> GetByIdAsync(Guid id);
    Task<IEnumerable<Cuota>> GetBySocioIdAsync(Guid socioId);
    Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false);
    Task<Cuota?> GetUltimaCuotaAsync(Guid socioId, Guid unidadId);
    Task<IEnumerable<Cuota>> GetCuotasParaRecordatorioAsync(DateTime hoy);
    /// <summary>
    /// Devuelve todas las cuotas pendientes (no anuladas) de TODOS los socios en una sola query.
    /// Usado para calcular el estado general de cuota de muchos socios sin caer en N+1.
    /// </summary>
    Task<IEnumerable<Cuota>> GetCuotasPendientesDeTodosLosSociosAsync(Guid? unidadId = null);
    /// <summary>
    /// RF-18: cuenta cuotas Pendientes con vencimiento dentro de [desde, hasta] (inclusive).
    /// <paramref name="unidadIds"/> null = todas las unidades.
    /// </summary>
    Task<int> CountPendientesPorVencerAsync(DateTime desde, DateTime hasta, IReadOnlyCollection<Guid>? unidadIds = null);
    /// <summary>RF-18: cuenta cuotas Pendientes ya vencidas (vencimiento anterior a <paramref name="hoy"/>).</summary>
    Task<int> CountPendientesVencidasAsync(DateTime hoy, IReadOnlyCollection<Guid>? unidadIds = null);
    /// <summary>RF-18: cuenta cuotas Pagadas con fecha de pago dentro del mes indicado.</summary>
    Task<int> CountPagadasDelMesAsync(int anio, int mes, IReadOnlyCollection<Guid>? unidadIds = null);
    Task AddAsync(Cuota cuota);
    Task DeletePendientesBySocioAsync(Guid socioId);
    Task SaveChangesAsync();
}
