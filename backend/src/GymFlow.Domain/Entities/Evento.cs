namespace GymFlow.Domain.Entities;

public class Evento
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
    public DateTime Fecha { get; private set; }
    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;
    public bool EstaActivo { get; private set; } = true;
    public DateTime FechaCreacion { get; private set; }

    private Evento() { } // EF Core

    public Evento(string titulo, string descripcion, DateTime fecha, Guid unidadId)
    {
        Id = Guid.NewGuid();
        // E2E-24 (barrido): sin paramName — el mensaje se muestra al usuario y no debe
        // llevar el sufijo "(Parameter '...')".
        Titulo = !string.IsNullOrWhiteSpace(titulo) ? titulo : throw new ArgumentException("El título es obligatorio.");
        Descripcion = descripcion ?? string.Empty;
        Fecha = fecha;
        UnidadId = unidadId;
        EstaActivo = true;
        FechaCreacion = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza los datos del evento. El dominio NO valida que la fecha sea futura:
    /// permitir ajustar a una fecha pasada es responsabilidad de la capa de aplicación.
    /// </summary>
    public void Actualizar(string titulo, string descripcion, DateTime fecha)
    {
        Titulo = !string.IsNullOrWhiteSpace(titulo) ? titulo : throw new ArgumentException("El título es obligatorio.");
        Descripcion = descripcion ?? string.Empty;
        Fecha = fecha;
    }

    /// <summary>Baja lógica. Idempotente: si ya está cancelado no hace nada ni lanza.</summary>
    public void Cancelar() => EstaActivo = false;

    /// <summary>Reactiva el evento. Idempotente: si ya está activo no hace nada ni lanza.</summary>
    public void Reactivar() => EstaActivo = true;
}
