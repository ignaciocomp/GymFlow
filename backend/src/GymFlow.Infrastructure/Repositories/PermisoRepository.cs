using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class PermisoRepository : IPermisoRepository
{
    private readonly GymFlowDbContext _db;
    public PermisoRepository(GymFlowDbContext db) => _db = db;

    public async Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Permisos.OrderBy(p => p.Modulo).ThenBy(p => p.Operacion).ToListAsync(ct);
}
