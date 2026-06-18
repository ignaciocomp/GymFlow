using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly GymFlowDbContext _db;

    public EmpleadoRepository(GymFlowDbContext db) => _db = db;

    public async Task<IReadOnlyList<Empleado>> GetAllAsync(bool? estaActivo = null, IReadOnlyCollection<Guid>? unidadesPermitidas = null, CancellationToken ct = default)
    {
        var query = _db.Set<Empleado>().Include(e => e.UnidadesAsignadas).AsQueryable();
        if (estaActivo.HasValue)
            query = query.Where(e => e.EstaActivo == estaActivo.Value);
        if (unidadesPermitidas is not null)
            query = query.Where(e => e.UnidadesAsignadas.Any(uu => unidadesPermitidas.Contains(uu.UnidadId)));
        return await query.OrderBy(e => e.Apellido).ThenBy(e => e.Nombre).ToListAsync(ct);
    }

    public Task<Empleado?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Set<Empleado>().Include(e => e.UnidadesAsignadas).FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Empleado?> GetByCorreoAsync(string correo, CancellationToken ct = default) =>
        _db.Set<Empleado>().Include(e => e.UnidadesAsignadas).FirstOrDefaultAsync(e => e.Correo == correo, ct);

    public async Task<bool> ExisteCorreoAsync(string correo, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.Set<Usuario>().AnyAsync(u => u.Correo == correo && (excludeId == null || u.Id != excludeId), ct);

    public async Task AddAsync(Empleado empleado, CancellationToken ct = default) =>
        await _db.Set<Empleado>().AddAsync(empleado, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
