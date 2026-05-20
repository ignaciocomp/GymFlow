namespace GymFlow.Domain.Entities;

public class InscripcionClase
{
    public Guid Id { get; private set; }
    public Guid ClaseId { get; private set; }
    public Clase Clase { get; private set; } = null!;
    public Guid SocioId { get; private set; }
    public Socio Socio { get; private set; } = null!;
    public DateTime FechaInscripcion { get; private set; }
    public bool EstaActiva { get; private set; } = true;

    private InscripcionClase() { } // EF Core

    public InscripcionClase(Guid claseId, Guid socioId)
    {
        Id = Guid.NewGuid();
        ClaseId = claseId;
        SocioId = socioId;
        FechaInscripcion = DateTime.UtcNow;
        EstaActiva = true;
    }

    public void Cancelar()
    {
        EstaActiva = false;
    }
}
