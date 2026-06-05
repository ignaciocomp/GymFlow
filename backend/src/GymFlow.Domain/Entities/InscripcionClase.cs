namespace GymFlow.Domain.Entities;

public class InscripcionClase
{
    public Guid Id { get; private set; }
    public Guid HorarioClaseId { get; private set; }
    public HorarioClase HorarioClase { get; private set; } = null!;
    public Guid SocioId { get; private set; }
    public Socio Socio { get; private set; } = null!;
    public DateTime FechaInscripcion { get; private set; }
    public bool EstaActiva { get; private set; } = true;
    public bool EsListaEspera { get; private set; }

    private InscripcionClase() { } // EF Core

    public InscripcionClase(Guid horarioClaseId, Guid socioId, bool esListaEspera = false)
    {
        Id = Guid.NewGuid();
        HorarioClaseId = horarioClaseId;
        SocioId = socioId;
        FechaInscripcion = DateTime.UtcNow;
        EstaActiva = true;
        EsListaEspera = esListaEspera;
    }

    public void Cancelar()
    {
        EstaActiva = false;
    }

    public void PromoverDeListaEspera()
    {
        if (!EsListaEspera)
            throw new InvalidOperationException("La inscripción no está en lista de espera.");
        EsListaEspera = false;
    }
}
