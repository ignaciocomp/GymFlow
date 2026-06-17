using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IEventoRepository
{
    Task<IEnumerable<Evento>> GetAllAsync(Guid? unidadId, bool incluirInactivos);
    Task<Evento?> GetByIdAsync(Guid id);
    Task<IEnumerable<Evento>> GetProximosByUnidadesAsync(IEnumerable<Guid> unidadIds, DateTime ahora);
    Task AddAsync(Evento evento);
    void Update(Evento evento);
    Task SaveChangesAsync();
}
