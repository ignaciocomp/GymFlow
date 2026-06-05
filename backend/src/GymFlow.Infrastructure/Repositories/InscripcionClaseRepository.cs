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
            .Include(i => i.HorarioClase)
                .ThenInclude(h => h.Clase)
                    .ThenInclude(c => c.Unidad)
            .Include(i => i.Socio)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.HorarioClase)
                .ThenInclude(h => h.Clase)
                    .ThenInclude(c => c.Unidad)
            .Where(i => i.SocioId == socioId && i.EstaActiva)
            .OrderByDescending(i => i.FechaInscripcion)
            .ToListAsync();
    }

    public async Task<InscripcionClase?> GetActivaBySocioYHorarioAsync(Guid socioId, Guid horarioClaseId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.HorarioClase)
                .ThenInclude(h => h.Clase)
                    .ThenInclude(c => c.Unidad)
            .FirstOrDefaultAsync(i => i.SocioId == socioId && i.HorarioClaseId == horarioClaseId && i.EstaActiva);
    }

    public async Task<int> GetInscripcionesActivasCountAsync(Guid horarioClaseId)
    {
        return await _context.InscripcionesClase
            .CountAsync(i => i.HorarioClaseId == horarioClaseId && i.EstaActiva && !i.EsListaEspera);
    }

    public async Task<InscripcionClase?> GetPrimeroEnListaEsperaAsync(Guid horarioClaseId) =>
        await _context.InscripcionesClase
            .Include(i => i.Socio)
            .Include(i => i.HorarioClase)
                .ThenInclude(h => h.Clase)
            .Where(i => i.HorarioClaseId == horarioClaseId && i.EstaActiva && i.EsListaEspera)
            .OrderBy(i => i.FechaInscripcion)
            .FirstOrDefaultAsync();

    public async Task<int> GetPosicionEnListaEsperaAsync(Guid inscripcionId)
    {
        var insc = await _context.InscripcionesClase.FindAsync(inscripcionId);
        if (insc is null || !insc.EsListaEspera) return 0;
        return await _context.InscripcionesClase.CountAsync(i =>
            i.HorarioClaseId == insc.HorarioClaseId && i.EstaActiva && i.EsListaEspera &&
            i.FechaInscripcion <= insc.FechaInscripcion);
    }

    public async Task<Dictionary<Guid, int>> GetConteoActivasPorHorariosAsync(IEnumerable<Guid> horarioClaseIds)
    {
        var ids = horarioClaseIds.Distinct().ToList();
        return await _context.InscripcionesClase
            .Where(i => ids.Contains(i.HorarioClaseId) && i.EstaActiva && !i.EsListaEspera)
            .GroupBy(i => i.HorarioClaseId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    public async Task<IEnumerable<InscripcionClase>> GetActivasByHorarioClaseIdAsync(Guid horarioClaseId)
    {
        return await _context.InscripcionesClase
            .Include(i => i.Socio)
            .Where(i => i.HorarioClaseId == horarioClaseId && i.EstaActiva)
            .ToListAsync();
    }

    public async Task AddAsync(InscripcionClase inscripcion) => await _context.InscripcionesClase.AddAsync(inscripcion);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
