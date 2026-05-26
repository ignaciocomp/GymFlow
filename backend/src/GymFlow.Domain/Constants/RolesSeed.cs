namespace GymFlow.Domain.Constants;

/// <summary>
/// Identidades fijas de los roles del sistema (seed). Vive en Domain para que tanto
/// Infrastructure (al sembrar la tabla) como Application (al validar reglas como
/// "no asignar rol Socio a Empleado") puedan referenciar una única fuente de verdad.
/// </summary>
public static class RolesSeed
{
    public static readonly Guid AdminRolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SocioRolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
}
