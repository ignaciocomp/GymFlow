using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Notificacion
{
    public Guid Id { get; private set; }
    public Guid SocioId { get; private set; }
    public Socio Socio { get; private set; } = null!;
    public TipoNotificacion Tipo { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Mensaje { get; private set; } = string.Empty;
    public bool Leida { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaLectura { get; private set; }

    private Notificacion() { } // EF Core

    public Notificacion(Guid socioId, TipoNotificacion tipo, string titulo, string mensaje)
    {
        Id = Guid.NewGuid();
        SocioId = socioId;
        Tipo = tipo;
        Titulo = !string.IsNullOrWhiteSpace(titulo) ? titulo : throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        Mensaje = !string.IsNullOrWhiteSpace(mensaje) ? mensaje : throw new ArgumentException("El mensaje es obligatorio.", nameof(mensaje));
        Leida = false;
        FechaCreacion = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca la notificación como leída. Idempotente: si ya está leída, no cambia la fecha de lectura.
    /// </summary>
    public void MarcarLeida(DateTime ahora)
    {
        if (Leida) return;
        Leida = true;
        FechaLectura = ahora;
    }
}
