using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class UnidadRepository : IUnidadRepository
{
    private readonly GymFlowDbContext _context;

    public UnidadRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Unidad>> GetAllAsync()
    {
        return await _context.Unidades.ToListAsync();
    }

    public async Task<Unidad?> GetByIdAsync(Guid id)
    {
        return await _context.Unidades.FindAsync(id);
    }
}
