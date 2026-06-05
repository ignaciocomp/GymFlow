namespace GymFlow.Domain.Entities;

public class Clase
{
    /// <summary>Capacidad máxima permitida por clase.</summary>
    public const int CapacidadMaximaPermitida = 500;

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
    public int CapacidadMaxima { get; private set; }
    public int DuracionMinutos { get; private set; }
    public string Instructor { get; private set; } = string.Empty;
    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;
    public bool EstaActivo { get; private set; } = true;

    private Clase() { } // EF Core

    public Clase(string nombre, string descripcion, int capacidadMaxima, int duracionMinutos, string instructor, Guid unidadId)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        Descripcion = descripcion ?? string.Empty;
        ValidarCapacidad(capacidadMaxima);
        CapacidadMaxima = capacidadMaxima;
        DuracionMinutos = duracionMinutos > 0 ? duracionMinutos : throw new ArgumentException("La duración debe ser mayor a 0.", nameof(duracionMinutos));
        Instructor = !string.IsNullOrWhiteSpace(instructor) ? instructor : throw new ArgumentException("El instructor es obligatorio.", nameof(instructor));
        UnidadId = unidadId;
        EstaActivo = true;
    }

    public void Actualizar(string nombre, string descripcion, int capacidadMaxima, int duracionMinutos, string instructor, int inscripcionesActivas)
    {
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        Descripcion = descripcion ?? string.Empty;

        ValidarCapacidad(capacidadMaxima);
        if (capacidadMaxima < inscripcionesActivas)
            throw new InvalidOperationException($"No se puede reducir la capacidad a {capacidadMaxima} porque hay {inscripcionesActivas} inscripciones activas.");

        CapacidadMaxima = capacidadMaxima;
        DuracionMinutos = duracionMinutos > 0 ? duracionMinutos : throw new ArgumentException("La duración debe ser mayor a 0.", nameof(duracionMinutos));
        Instructor = !string.IsNullOrWhiteSpace(instructor) ? instructor : throw new ArgumentException("El instructor es obligatorio.", nameof(instructor));
    }

    public void Cancelar()
    {
        if (!EstaActivo)
            throw new InvalidOperationException("La clase ya está cancelada.");
        EstaActivo = false;
    }

    /// <summary>
    /// Reactiva una clase previamente cancelada.
    /// Las inscripciones que fueron canceladas al momento de la baja NO se restauran
    /// automáticamente; los socios deben volver a inscribirse.
    /// </summary>
    public void Reactivar()
    {
        if (EstaActivo)
            throw new InvalidOperationException("La clase ya está activa.");
        EstaActivo = true;
    }

    private static void ValidarCapacidad(int capacidadMaxima)
    {
        if (capacidadMaxima <= 0)
            throw new ArgumentException("La capacidad máxima debe ser mayor a 0.", nameof(capacidadMaxima));
        if (capacidadMaxima > CapacidadMaximaPermitida)
            throw new ArgumentException($"La capacidad máxima no puede superar {CapacidadMaximaPermitida}.", nameof(capacidadMaxima));
    }
}
