using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class EventoRepository : IEventoRepository
{
    private readonly GymFlowDbContext _context;

    public EventoRepository(GymFlowDbContext context) => _context = context;

    public async Task<IEnumerable<Evento>> GetAllAsync(Guid? unidadId, bool incluirInactivos)
    {
        var query = _context.Eventos
            .Include(e => e.Unidad)
            .AsQueryable();

        if (unidadId.HasValue)
            query = query.Where(e => e.UnidadId == unidadId.Value);

        if (!incluirInactivos)
            query = query.Where(e => e.EstaActivo);

        return await query.OrderBy(e => e.Fecha).ToListAsync();
    }

    public async Task<Evento?> GetByIdAsync(Guid id)
    {
        return await _context.Eventos
            .Include(e => e.Unidad)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Evento>> GetProximosByUnidadesAsync(IEnumerable<Guid> unidadIds, DateTime ahora)
    {
        var ids = unidadIds.ToList();

        return await _context.Eventos
            .Include(e => e.Unidad)
            .Where(e => e.EstaActivo && e.Fecha >= ahora && ids.Contains(e.UnidadId))
            .OrderBy(e => e.Fecha)
            .ToListAsync();
    }

    public async Task AddAsync(Evento evento) => await _context.Eventos.AddAsync(evento);

    public void Update(Evento evento) => _context.Eventos.Update(evento);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
