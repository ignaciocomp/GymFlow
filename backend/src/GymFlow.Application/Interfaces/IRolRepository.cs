using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IRolRepository
{
    Task<IReadOnlyList<Rol>> GetAllAsync(CancellationToken ct = default);
    Task<Rol?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Rol?> GetByNombreAsync(string nombre, CancellationToken ct = default);
    Task<bool> ExisteConNombreAsync(string nombre, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> TieneUsuariosActivosAsignadosAsync(Guid rolId, CancellationToken ct = default);
    Task AddAsync(Rol rol, CancellationToken ct = default);
    void Remove(Rol rol);
    Task SaveChangesAsync(CancellationToken ct = default);
}
