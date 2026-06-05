using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class ClaseRepository : IClaseRepository
{
    private readonly GymFlowDbContext _context;

    public ClaseRepository(GymFlowDbContext context) => _context = context;

    public async Task<IEnumerable<Clase>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Clases
            .Include(c => c.Unidad)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.EstaActivo);

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Clase?> GetByIdAsync(Guid id)
    {
        return await _context.Clases
            .Include(c => c.Unidad)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Clase>> GetByUnidadIdAsync(Guid unidadId, bool includeInactive = false)
    {
        var query = _context.Clases
            .Include(c => c.Unidad)
            .Where(c => c.UnidadId == unidadId);

        if (!includeInactive)
            query = query.Where(c => c.EstaActivo);

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<int> GetInscripcionesActivasCountAsync(Guid claseId)
    {
        return await _context.InscripcionesClase
            .CountAsync(i => i.ClaseId == claseId && i.EstaActiva && !i.EsListaEspera);
    }

    public async Task<IEnumerable<InscripcionClase>> GetInscripcionesActivasAsync(Guid claseId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.Socio)
            .Where(i => i.ClaseId == claseId && i.EstaActiva)
            .ToListAsync();
    }

    public async Task AddAsync(Clase clase) => await _context.Clases.AddAsync(clase);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
