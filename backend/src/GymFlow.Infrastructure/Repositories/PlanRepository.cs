using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly GymFlowDbContext _context;

    public PlanRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Plan>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Planes
            .Include(p => p.Unidad)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.EstaActivo);

        return await query.OrderBy(p => p.Nombre).ToListAsync();
    }

    public async Task<Plan?> GetByIdAsync(Guid id)
    {
        return await _context.Planes
            .Include(p => p.Unidad)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Plan>> GetByUnidadIdAsync(Guid unidadId)
    {
        return await _context.Planes
            .Include(p => p.Unidad)
            .Where(p => p.UnidadId == unidadId && p.EstaActivo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteSocioConPlanAsync(Guid planId)
    {
        return await _context.UsuarioUnidades.AnyAsync(uu => uu.PlanId == planId);
    }

    public async Task AddAsync(Plan plan)
    {
        await _context.Planes.AddAsync(plan);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
