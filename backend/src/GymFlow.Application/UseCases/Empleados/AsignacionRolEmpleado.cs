using GymFlow.Domain.Constants;

namespace GymFlow.Application.UseCases.Empleados;

/// <summary>
/// Reglas compartidas para asignar un rol a un empleado, validadas con el rol del actuante.
/// Las usan Crear/Actualizar/Reactivar para que la regla "solo el Admin crea Dueños" no se
/// pueda saltear por ninguna de las tres vías.
/// </summary>
internal static class AsignacionRolEmpleado
{
    /// <summary>
    /// Valida la asignación de <paramref name="rolAsignadoId"/> a un empleado por parte de un
    /// actuante con rol <paramref name="actuanteRolId"/>, asignándole <paramref name="unidadesAsignadas"/>.
    /// </summary>
    /// <param name="actuanteUnidades">
    /// Unidades del actuante cuando es Dueño (para validar que asigna un subconjunto de las suyas);
    /// null cuando el actuante es Admin u otro rol sin restricción de unidad.
    /// </param>
    public static void ValidarAsignacion(
        Guid rolAsignadoId,
        Guid actuanteRolId,
        IReadOnlyCollection<Guid> unidadesAsignadas,
        IReadOnlyCollection<Guid>? actuanteUnidades)
    {
        // Solo el Admin puede asignar (o reactivar a) el rol Dueño.
        if (rolAsignadoId == RolesSeed.DuenoRolId && actuanteRolId != RolesSeed.AdminRolId)
            throw new UnauthorizedAccessException("Solo el administrador puede asignar el rol Dueño.");

        // Un empleado Dueño debe tener al menos una unidad asignada.
        if (rolAsignadoId == RolesSeed.DuenoRolId && unidadesAsignadas.Count == 0)
            throw new ArgumentException("Un Dueño debe tener al menos una unidad asignada.");

        // Un Dueño solo puede asignar unidades dentro de las suyas.
        if (actuanteRolId == RolesSeed.DuenoRolId && actuanteUnidades is not null)
        {
            var permitidas = actuanteUnidades.ToHashSet();
            if (unidadesAsignadas.Any(u => !permitidas.Contains(u)))
                throw new ArgumentException("No se pueden asignar unidades fuera de las del Dueño actuante.");
        }
    }

    /// <summary>
    /// Verifica que solo el Admin pueda asignar el rol Dueño. Se usa al reactivar (donde no se
    /// reasignan unidades) para cubrir el bypass de la regla "solo el Admin crea Dueños".
    /// </summary>
    public static void ValidarSoloAdminAsignaDueno(Guid rolAsignadoId, Guid actuanteRolId)
    {
        if (rolAsignadoId == RolesSeed.DuenoRolId && actuanteRolId != RolesSeed.AdminRolId)
            throw new UnauthorizedAccessException("Solo el administrador puede asignar el rol Dueño.");
    }
}
