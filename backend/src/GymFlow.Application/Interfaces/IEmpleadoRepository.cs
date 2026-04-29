using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IEmpleadoRepository
{
    Task<IReadOnlyList<Empleado>> GetAllAsync(bool? estaActivo = null, CancellationToken ct = default);
    Task<Empleado?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Empleado?> GetByCorreoAsync(string correo, CancellationToken ct = default);
    Task<bool> ExisteCorreoAsync(string correo, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Empleado empleado, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
