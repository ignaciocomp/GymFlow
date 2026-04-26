using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IPermisoCache
{
    Task<bool> TienePermisoAsync(Guid rolId, Modulo modulo, Operacion operacion, CancellationToken ct = default);
    Task<IReadOnlyList<(Modulo Modulo, Operacion Operacion)>> ObtenerPermisosAsync(Guid rolId, CancellationToken ct = default);
    void Invalidar(Guid rolId);
}
