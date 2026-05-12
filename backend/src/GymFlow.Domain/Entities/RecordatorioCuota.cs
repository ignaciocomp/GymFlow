using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class RecordatorioCuota
{
    public Guid Id { get; private set; }
    public Guid CuotaId { get; private set; }
    public Cuota Cuota { get; private set; } = null!;
    public Guid SocioId { get; private set; }
    public Socio Socio { get; private set; } = null!;
    public TipoRecordatorio TipoRecordatorio { get; private set; }
    public DateTime FechaEnvio { get; private set; }
    public bool Exitoso { get; private set; }
    public string? Error { get; private set; }

    private RecordatorioCuota() { } // EF Core

    public RecordatorioCuota(Guid cuotaId, Guid socioId, TipoRecordatorio tipo, bool exitoso, string? error = null)
    {
        Id = Guid.NewGuid();
        CuotaId = cuotaId;
        SocioId = socioId;
        TipoRecordatorio = tipo;
        FechaEnvio = DateTime.UtcNow;
        Exitoso = exitoso;
        Error = error;
    }
}
