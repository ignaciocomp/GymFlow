namespace GymFlow.Domain.Entities;

public class UsuarioUnidad
{
    public Guid UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;

    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;

    private UsuarioUnidad() { } // EF Core

    public UsuarioUnidad(Guid usuarioId, Guid unidadId)
    {
        UsuarioId = usuarioId;
        UnidadId = unidadId;
    }
}
