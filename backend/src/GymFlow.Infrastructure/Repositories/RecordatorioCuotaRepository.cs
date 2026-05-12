using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class RecordatorioCuotaRepository : IRecordatorioCuotaRepository
{
    private readonly GymFlowDbContext _context;

    public RecordatorioCuotaRepository(GymFlowDbContext context) => _context = context;

    public async Task AddAsync(RecordatorioCuota recordatorio)
    {
        await _context.RecordatoriosCuota.AddAsync(recordatorio);
    }

    public async Task<bool> ExisteRecordatorioHoyAsync(Guid cuotaId, TipoRecordatorio tipo)
    {
        var hoy = DateTime.UtcNow.Date;
        var manana = hoy.AddDays(1);

        return await _context.RecordatoriosCuota.AnyAsync(r =>
            r.CuotaId == cuotaId &&
            r.TipoRecordatorio == tipo &&
            r.FechaEnvio >= hoy &&
            r.FechaEnvio < manana);
    }

    public async Task<IEnumerable<RecordatorioCuota>> GetByCuotaIdAsync(Guid cuotaId)
    {
        return await _context.RecordatoriosCuota
            .Where(r => r.CuotaId == cuotaId)
            .OrderByDescending(r => r.FechaEnvio)
            .ToListAsync();
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
