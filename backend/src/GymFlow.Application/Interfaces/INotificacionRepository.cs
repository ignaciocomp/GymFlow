using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface INotificacionRepository
{
    Task AddRangeAsync(IEnumerable<Notificacion> notificaciones);
    Task<IEnumerable<Notificacion>> GetBySocioAsync(Guid socioId, bool soloNoLeidas, int take);
    Task<int> ContarNoLeidasAsync(Guid socioId);
    Task<Notificacion?> GetByIdAsync(Guid id);
    Task MarcarTodasLeidasAsync(Guid socioId, DateTime ahora);
    Task SaveChangesAsync();
}
