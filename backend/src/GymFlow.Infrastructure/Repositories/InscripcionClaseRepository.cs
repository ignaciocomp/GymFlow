using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class InscripcionClaseRepository : IInscripcionClaseRepository
{
    private readonly GymFlowDbContext _context;

    public InscripcionClaseRepository(GymFlowDbContext context) => _context = context;

    public async Task<InscripcionClase?> GetByIdAsync(Guid id)
    {
        return await _context.InscripcionesClase
            .Include(i => i.Clase)
                .ThenInclude(c => c.Unidad)
            .Include(i => i.Socio)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.Clase)
                .ThenInclude(c => c.Unidad)
            .Where(i => i.SocioId == socioId && i.EstaActiva)
            .OrderByDescending(i => i.FechaInscripcion)
            .ToListAsync();
    }

    public async Task<InscripcionClase?> GetActivaBySocioYClaseAsync(Guid socioId, Guid claseId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.Clase)
                .ThenInclude(c => c.Unidad)
            .FirstOrDefaultAsync(i => i.SocioId == socioId && i.ClaseId == claseId && i.EstaActiva);
    }

    public async Task<int> GetInscripcionesActivasCountAsync(Guid claseId)
    {
        return await _context.InscripcionesClase
            .CountAsync(i => i.ClaseId == claseId && i.EstaActiva);
    }

    public async Task AddAsync(InscripcionClase inscripcion) => await _context.InscripcionesClase.AddAsync(inscripcion);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
