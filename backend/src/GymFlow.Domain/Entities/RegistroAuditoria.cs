using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class RegistroAuditoria
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string UsuarioNombre { get; private set; } = string.Empty;
    public TipoAccionAuditoria TipoAccion { get; private set; }
    public string EntidadAfectada { get; private set; } = string.Empty;
    public Guid? EntidadId { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public string? DetallesCambios { get; private set; }
    public DateTime FechaHora { get; private set; }

    private RegistroAuditoria() { } // EF Core

    public RegistroAuditoria(
        Guid usuarioId,
        string usuarioNombre,
        TipoAccionAuditoria tipoAccion,
        string entidadAfectada,
        Guid? entidadId,
        string descripcion,
        string? detallesCambios = null)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        UsuarioNombre = !string.IsNullOrWhiteSpace(usuarioNombre)
            ? usuarioNombre
            : throw new ArgumentException("UsuarioNombre is required.", nameof(usuarioNombre));
        TipoAccion = tipoAccion;
        EntidadAfectada = !string.IsNullOrWhiteSpace(entidadAfectada)
            ? entidadAfectada
            : throw new ArgumentException("EntidadAfectada is required.", nameof(entidadAfectada));
        EntidadId = entidadId;
        Descripcion = !string.IsNullOrWhiteSpace(descripcion)
            ? descripcion
            : throw new ArgumentException("Descripcion is required.", nameof(descripcion));
        DetallesCambios = detallesCambios;
        FechaHora = DateTime.UtcNow;
    }
}
