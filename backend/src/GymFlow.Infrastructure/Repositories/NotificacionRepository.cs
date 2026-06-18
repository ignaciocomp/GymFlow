using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class NotificacionRepository : INotificacionRepository
{
    private readonly GymFlowDbContext _context;

    public NotificacionRepository(GymFlowDbContext context) => _context = context;

    public async Task AddRangeAsync(IEnumerable<Notificacion> notificaciones)
        => await _context.Notificaciones.AddRangeAsync(notificaciones);

    public async Task<IEnumerable<Notificacion>> GetBySocioAsync(Guid socioId, bool soloNoLeidas, int take)
    {
        var query = _context.Notificaciones
            .Where(n => n.SocioId == socioId);

        if (soloNoLeidas)
            query = query.Where(n => !n.Leida);

        return await query
            .OrderByDescending(n => n.FechaCreacion)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> ContarNoLeidasAsync(Guid socioId)
        => await _context.Notificaciones.CountAsync(n => n.SocioId == socioId && !n.Leida);

    public async Task<Notificacion?> GetByIdAsync(Guid id)
        => await _context.Notificaciones.FirstOrDefaultAsync(n => n.Id == id);

    public async Task MarcarTodasLeidasAsync(Guid socioId, DateTime ahora)
    {
        var noLeidas = await _context.Notificaciones
            .Where(n => n.SocioId == socioId && !n.Leida)
            .ToListAsync();

        foreach (var notificacion in noLeidas)
            notificacion.MarcarLeida(ahora);
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
