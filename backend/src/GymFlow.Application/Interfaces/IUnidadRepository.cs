using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IUnidadRepository
{
    Task<IEnumerable<Unidad>> GetAllAsync();
    Task<Unidad?> GetByIdAsync(Guid id);
}
