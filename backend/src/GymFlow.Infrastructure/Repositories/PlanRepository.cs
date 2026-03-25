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

    public async Task<IEnumerable<Plan>> GetAllAsync()
    {
        return await _context.Planes
            .Where(p => p.EstaActivo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Plan?> GetByIdAsync(Guid id)
    {
        return await _context.Planes.FindAsync(id);
    }

    public async Task<IEnumerable<Plan>> GetByUnidadIdAsync(Guid unidadId)
    {
        return await _context.Planes
            .Where(p => p.UnidadId == unidadId && p.EstaActivo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }
}
