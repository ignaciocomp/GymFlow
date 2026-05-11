using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class CuotaTests
{
    private static Cuota CrearCuotaValida() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Musculación", 2500m, DateTime.UtcNow);

    [Fact]
    public void Constructor_WithValidData_CreatesCuota()
    {
        var fechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Completo", 3500m, fechaEmision);

        Assert.NotEqual(Guid.Empty, cuota.Id);
        Assert.Equal("Plan Completo", cuota.NombrePlan);
        Assert.Equal(3500m, cuota.Monto);
        Assert.Equal(fechaEmision, cuota.FechaEmision);
        Assert.Equal(fechaEmision.AddMonths(1), cuota.FechaVencimiento);
        Assert.Equal(EstadoCuota.Pendiente, cuota.Estado);
        Assert.Null(cuota.FechaPago);
        Assert.Null(cuota.FechaBaja);
    }

    [Fact]
    public void Constructor_WithNegativeMonto_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan", -100m, DateTime.UtcNow));
    }

    [Fact]
    public void MarcarComoPagada_WhenPendiente_ChangesEstadoAndSetsFechaPago()
    {
        var cuota = CrearCuotaValida();
        cuota.MarcarComoPagada();

        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
        Assert.NotNull(cuota.FechaPago);
    }

    [Fact]
    public void MarcarComoPagada_WhenAlreadyPagada_ThrowsInvalidOperationException()
    {
        var cuota = CrearCuotaValida();
        cuota.MarcarComoPagada();

        Assert.Throws<InvalidOperationException>(() => cuota.MarcarComoPagada());
    }

    [Fact]
    public void Anular_WhenPendiente_SetsFechaBaja()
    {
        var cuota = CrearCuotaValida();
        cuota.Anular();

        Assert.NotNull(cuota.FechaBaja);
    }

    [Fact]
    public void Anular_WhenPagada_ThrowsInvalidOperationException()
    {
        var cuota = CrearCuotaValida();
        cuota.MarcarComoPagada();

        Assert.Throws<InvalidOperationException>(() => cuota.Anular());
    }

    [Fact]
    public void Constructor_WithDay31_AdjustsVencimientoToLastDayOfMonth()
    {
        var fechaEmision = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, fechaEmision);

        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), cuota.FechaVencimiento);
    }

    [Fact]
    public void RevertirPago_WhenPagada_ChangesEstadoToPendienteAndClearsFechaPago()
    {
        var cuota = CrearCuotaValida();
        cuota.MarcarComoPagada();

        cuota.RevertirPago();

        Assert.Equal(EstadoCuota.Pendiente, cuota.Estado);
        Assert.Null(cuota.FechaPago);
    }

    [Fact]
    public void RevertirPago_WhenPendiente_ThrowsInvalidOperationException()
    {
        var cuota = CrearCuotaValida();

        Assert.Throws<InvalidOperationException>(() => cuota.RevertirPago());
    }

    [Fact]
    public void RevertirAnulacion_WhenAnulada_ClearsFechaBaja()
    {
        var cuota = CrearCuotaValida();
        cuota.Anular();

        cuota.RevertirAnulacion();

        Assert.Null(cuota.FechaBaja);
    }

    [Fact]
    public void RevertirAnulacion_WhenNotAnulada_ThrowsInvalidOperationException()
    {
        var cuota = CrearCuotaValida();

        Assert.Throws<InvalidOperationException>(() => cuota.RevertirAnulacion());
    }
}
