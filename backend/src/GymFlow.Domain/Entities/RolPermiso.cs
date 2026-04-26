namespace GymFlow.Domain.Entities;

public class RolPermiso
{
    public Guid RolId { get; private set; }
    public Rol Rol { get; private set; } = null!;
    public Guid PermisoId { get; private set; }
    public Permiso Permiso { get; private set; } = null!;

    private RolPermiso() { } // EF Core

    public RolPermiso(Guid rolId, Guid permisoId)
    {
        RolId = rolId;
        PermisoId = permisoId;
    }
}
