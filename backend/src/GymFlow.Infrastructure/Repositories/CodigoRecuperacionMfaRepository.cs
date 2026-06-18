using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class CodigoRecuperacionMfaRepository : ICodigoRecuperacionMfaRepository
{
    private readonly GymFlowDbContext _db;

    public CodigoRecuperacionMfaRepository(GymFlowDbContext db) => _db = db;

    public async Task AgregarRangoAsync(IEnumerable<CodigoRecuperacionMfa> codigos) =>
        await _db.CodigosRecuperacionMfa.AddRangeAsync(codigos);

    public async Task<IReadOnlyList<CodigoRecuperacionMfa>> GetActivosPorEmpleadoAsync(Guid empleadoId) =>
        await _db.CodigosRecuperacionMfa
            .Where(c => c.EmpleadoId == empleadoId && !c.Usado)
            .ToListAsync();

    public async Task EliminarPorEmpleadoAsync(Guid empleadoId)
    {
        var codigos = await _db.CodigosRecuperacionMfa
            .Where(c => c.EmpleadoId == empleadoId)
            .ToListAsync();

        _db.CodigosRecuperacionMfa.RemoveRange(codigos);
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
