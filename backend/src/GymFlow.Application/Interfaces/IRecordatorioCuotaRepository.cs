using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IRecordatorioCuotaRepository
{
    Task AddAsync(RecordatorioCuota recordatorio);
    Task<bool> ExisteRecordatorioHoyAsync(Guid cuotaId, TipoRecordatorio tipo);
    /// <summary>
    /// Igual que ExisteRecordatorioHoyAsync pero solo cuenta los envíos que fueron exitosos.
    /// Usado para permitir reintentos manuales cuando el envío anterior falló (timeout SMTP, etc.).
    /// </summary>
    Task<bool> ExisteRecordatorioExitosoHoyAsync(Guid cuotaId, TipoRecordatorio tipo);
    Task<IEnumerable<RecordatorioCuota>> GetByCuotaIdAsync(Guid cuotaId);
    Task SaveChangesAsync();
}
