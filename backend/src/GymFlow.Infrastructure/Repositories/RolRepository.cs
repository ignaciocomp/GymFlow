using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class RolRepository : IRolRepository
{
    private readonly GymFlowDbContext _db;

    public RolRepository(GymFlowDbContext db) => _db = db;

    public async Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Roles
            .Include(r => r.Permisos).ThenInclude(rp => rp.Permiso)
            .OrderBy(r => r.Nombre)
            .ToListAsync(ct);

    public async Task<Rol?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Roles
            .Include(r => r.Permisos).ThenInclude(rp => rp.Permiso)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Rol?> GetByNombreAsync(string nombre, CancellationToken ct = default) =>
        await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre, ct);

    public async Task<bool> ExisteConNombreAsync(string nombre, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.Roles.AnyAsync(r => r.Nombre == nombre && (excludeId == null || r.Id != excludeId), ct);

    public async Task<bool> TieneUsuariosActivosAsignadosAsync(Guid rolId, CancellationToken ct = default) =>
        await _db.Set<Usuario>().AnyAsync(u => u.RolId == rolId && u.EstaActivo, ct);

    public async Task AddAsync(Rol rol, CancellationToken ct = default) =>
        await _db.Roles.AddAsync(rol, ct);

    public void Remove(Rol rol) => _db.Roles.Remove(rol);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
