using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IPermisoRepository
{
    Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken ct = default);
}
