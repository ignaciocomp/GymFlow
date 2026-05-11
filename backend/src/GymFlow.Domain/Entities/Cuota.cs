using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Cuota
{
    public Guid Id { get; private set; }
    public Guid SocioId { get; private set; }
    public Socio Socio { get; private set; } = null!;
    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;
    public Guid PlanId { get; private set; }
    public Plan Plan { get; private set; } = null!;
    public string NombrePlan { get; private set; } = string.Empty;
    public decimal Monto { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public DateTime FechaVencimiento { get; private set; }
    public EstadoCuota Estado { get; private set; }
    public DateTime? FechaPago { get; private set; }
    public DateTime? FechaBaja { get; private set; }

    private Cuota() { } // EF Core

    public Cuota(Guid socioId, Guid unidadId, Guid planId, string nombrePlan, decimal monto, DateTime fechaEmision)
    {
        if (string.IsNullOrWhiteSpace(nombrePlan))
            throw new ArgumentException("El nombre del plan es requerido.", nameof(nombrePlan));
        if (monto < 0)
            throw new ArgumentException("El monto no puede ser negativo.", nameof(monto));

        Id = Guid.NewGuid();
        SocioId = socioId;
        UnidadId = unidadId;
        PlanId = planId;
        NombrePlan = nombrePlan;
        Monto = monto;
        FechaEmision = fechaEmision;
        FechaVencimiento = fechaEmision.AddMonths(1);
        Estado = EstadoCuota.Pendiente;
    }

    public void MarcarComoPagada()
    {
        if (Estado == EstadoCuota.Pagada)
            throw new InvalidOperationException("La cuota ya está pagada.");
        if (FechaBaja.HasValue)
            throw new InvalidOperationException("No se puede pagar una cuota anulada.");

        Estado = EstadoCuota.Pagada;
        FechaPago = DateTime.UtcNow;
    }

    public void RevertirPago()
    {
        if (Estado != EstadoCuota.Pagada)
            throw new InvalidOperationException("Solo se puede revertir el pago de una cuota pagada.");
        if (FechaBaja.HasValue)
            throw new InvalidOperationException("No se puede revertir el pago de una cuota anulada.");

        Estado = EstadoCuota.Pendiente;
        FechaPago = null;
    }

    public void Anular()
    {
        if (Estado == EstadoCuota.Pagada)
            throw new InvalidOperationException("No se puede anular una cuota ya pagada.");
        if (FechaBaja.HasValue)
            throw new InvalidOperationException("La cuota ya fue anulada.");

        FechaBaja = DateTime.UtcNow;
    }

    public void RevertirAnulacion()
    {
        if (!FechaBaja.HasValue)
            throw new InvalidOperationException("La cuota no está anulada.");

        FechaBaja = null;
    }
}
