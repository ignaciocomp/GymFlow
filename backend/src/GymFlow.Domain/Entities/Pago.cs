using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Pago
{
    public Guid Id { get; private set; }
    public Guid CuotaId { get; private set; }
    public Cuota Cuota { get; private set; } = null!;
    public Guid SocioId { get; private set; }
    public decimal Monto { get; private set; }
    public EstadoPago Estado { get; private set; }
    public string? MedioPago { get; private set; }
    public string MpPreferenceId { get; private set; } = string.Empty;
    public string? MpPaymentId { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaAcreditacion { get; private set; }

    private Pago() { } // EF Core

    public Pago(Guid cuotaId, Guid socioId, decimal monto, string mpPreferenceId)
    {
        if (monto < 0)
            throw new ArgumentException("El monto no puede ser negativo.", nameof(monto));

        Id = Guid.NewGuid();
        CuotaId = cuotaId;
        SocioId = socioId;
        Monto = monto;
        MpPreferenceId = mpPreferenceId ?? string.Empty;
        Estado = EstadoPago.Pendiente;
        FechaCreacion = DateTime.UtcNow;
    }

    public void SetMpPreferenceId(string mpPreferenceId)
    {
        MpPreferenceId = mpPreferenceId ?? string.Empty;
    }

    public void MarcarAprobado(string mpPaymentId, string? medioPago)
    {
        if (Estado != EstadoPago.Pendiente)
            throw new InvalidOperationException($"No se puede aprobar un pago en estado {Estado}.");

        Estado = EstadoPago.Aprobado;
        MpPaymentId = mpPaymentId;
        MedioPago = medioPago;
        FechaAcreditacion = DateTime.UtcNow;
    }

    public void MarcarRechazado()
    {
        if (Estado != EstadoPago.Pendiente)
            throw new InvalidOperationException($"No se puede rechazar un pago en estado {Estado}.");

        Estado = EstadoPago.Rechazado;
    }
}
