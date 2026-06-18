using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class HorarioClaseRepository : IHorarioClaseRepository
{
    private readonly GymFlowDbContext _context;

    public HorarioClaseRepository(GymFlowDbContext context) => _context = context;

    public async Task<IEnumerable<HorarioClase>> GetAllAsync(Guid? unidadId = null, IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        var query = _context.HorariosClase
            .Include(h => h.Clase)
                .ThenInclude(c => c.Unidad)
            .AsQueryable();

        if (unidadesPermitidas is not null)
            query = query.Where(h => unidadesPermitidas.Contains(h.Clase.UnidadId));

        if (unidadId.HasValue)
            query = query.Where(h => h.Clase.UnidadId == unidadId.Value);

        return await query
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .ToListAsync();
    }

    public async Task<HorarioClase?> GetByIdAsync(Guid id)
    {
        return await _context.HorariosClase
            .Include(h => h.Clase)
                .ThenInclude(c => c.Unidad)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<IEnumerable<HorarioClase>> GetByClaseIdAsync(Guid claseId)
    {
        return await _context.HorariosClase
            .Include(h => h.Clase)
                .ThenInclude(c => c.Unidad)
            .Where(h => h.ClaseId == claseId)
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .ToListAsync();
    }

    public async Task<IEnumerable<HorarioClase>> GetByUnidadYDiaAsync(Guid unidadId, DiaSemana dia)
    {
        return await _context.HorariosClase
            .Include(h => h.Clase)
            .Where(h => h.Clase.UnidadId == unidadId && h.DiaSemana == dia)
            .ToListAsync();
    }

    public async Task AddAsync(HorarioClase horario) => await _context.HorariosClase.AddAsync(horario);

    public void Remove(HorarioClase horario) => _context.HorariosClase.Remove(horario);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
