using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IRecordatorioCuotaRepository
{
    Task AddAsync(RecordatorioCuota recordatorio);
    Task<bool> ExisteRecordatorioHoyAsync(Guid cuotaId, TipoRecordatorio tipo);
    Task<IEnumerable<RecordatorioCuota>> GetByCuotaIdAsync(Guid cuotaId);
    Task SaveChangesAsync();
}
