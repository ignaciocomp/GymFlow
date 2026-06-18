using GymFlow.Application.Interfaces;
using GymFlow.Domain.Constants;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IUnidadesVisiblesResolver"/>. Resuelve las unidades del Dueño
/// vía <see cref="IEmpleadoRepository.GetByIdAsync"/> (que ya incluye <c>UnidadesAsignadas</c>).
/// </summary>
public class UnidadesVisiblesResolver : IUnidadesVisiblesResolver
{
    private readonly IEmpleadoRepository _empleadoRepository;

    public UnidadesVisiblesResolver(IEmpleadoRepository empleadoRepository) =>
        _empleadoRepository = empleadoRepository;

    public async Task<IReadOnlyCollection<Guid>?> ResolverAsync(Guid userId, Guid rolId, CancellationToken ct = default)
    {
        if (rolId == RolesSeed.AdminRolId)
            return null; // Admin: sin restricción

        if (rolId != RolesSeed.DuenoRolId)
            return null; // Otros roles: el acceso lo gobiernan los permisos de módulo

        var empleado = await _empleadoRepository.GetByIdAsync(userId, ct);
        if (empleado is null)
            return Array.Empty<Guid>();

        return empleado.UnidadesAsignadas.Select(uu => uu.UnidadId).Distinct().ToArray();
    }
}
