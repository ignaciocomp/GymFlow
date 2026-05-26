namespace GymFlow.Domain.Entities;

public class UsuarioUnidad
{
    public Guid UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;

    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;

    public Guid? PlanId { get; private set; }
    public Plan? Plan { get; private set; }

    private UsuarioUnidad() { } // EF Core

    public UsuarioUnidad(Guid usuarioId, Guid unidadId, Guid? planId = null)
    {
        UsuarioId = usuarioId;
        UnidadId = unidadId;
        PlanId = planId;
    }

    public void AsignarPlan(Guid? planId)
    {
        PlanId = planId;
    }
}
