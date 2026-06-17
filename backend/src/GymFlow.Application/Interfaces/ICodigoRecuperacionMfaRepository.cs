using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface ICodigoRecuperacionMfaRepository
{
    Task AgregarRangoAsync(IEnumerable<CodigoRecuperacionMfa> codigos);
    Task<IReadOnlyList<CodigoRecuperacionMfa>> GetActivosPorEmpleadoAsync(Guid empleadoId);
    Task EliminarPorEmpleadoAsync(Guid empleadoId);
    Task SaveChangesAsync();
}
