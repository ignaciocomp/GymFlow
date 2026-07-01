using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class PagoTests
{
    private static Pago NuevoPago() => new(Guid.NewGuid(), Guid.NewGuid(), 1500m, "pref-123");

    [Fact]
    public void NuevoPago_QuedaPendiente()
    {
        var p = NuevoPago();
        Assert.Equal(EstadoPago.Pendiente, p.Estado);
        Assert.Equal("pref-123", p.MpPreferenceId);
        Assert.Null(p.MpPaymentId);
        Assert.Null(p.MedioPago);
        Assert.Null(p.FechaAcreditacion);
        Assert.NotEqual(Guid.Empty, p.Id);
        Assert.NotEqual(default, p.FechaCreacion);
    }

    [Fact]
    public void Constructor_WithNegativeMonto_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Pago(Guid.NewGuid(), Guid.NewGuid(), -100m, "pref-123"));
    }

    [Fact]
    public void MarcarAprobado_SeteaDatos()
    {
        var p = NuevoPago();
        p.MarcarAprobado("mp-999", "credit_card");
        Assert.Equal(EstadoPago.Aprobado, p.Estado);
        Assert.Equal("mp-999", p.MpPaymentId);
        Assert.Equal("credit_card", p.MedioPago);
        Assert.NotNull(p.FechaAcreditacion);
    }

    [Fact]
    public void MarcarAprobado_DosVeces_Lanza()
    {
        var p = NuevoPago();
        p.MarcarAprobado("mp-999", "credit_card");
        Assert.Throws<InvalidOperationException>(() => p.MarcarAprobado("mp-1000", "x"));
    }

    [Fact]
    public void MarcarRechazado_WhenPendiente_PasaARechazado()
    {
        var p = NuevoPago();
        p.MarcarRechazado();
        Assert.Equal(EstadoPago.Rechazado, p.Estado);
        Assert.Null(p.FechaAcreditacion);
        Assert.Null(p.MpPaymentId);
    }

    [Fact]
    public void MarcarRechazado_CuandoYaAprobado_Lanza()
    {
        var p = NuevoPago();
        p.MarcarAprobado("mp-999", "credit_card");
        Assert.Throws<InvalidOperationException>(() => p.MarcarRechazado());
    }

    [Fact]
    public void MarcarAprobado_CuandoRechazado_Lanza()
    {
        var p = NuevoPago();
        p.MarcarRechazado();
        Assert.Throws<InvalidOperationException>(() => p.MarcarAprobado("mp-999", "credit_card"));
    }

    [Fact]
    public void SetMpPreferenceId_ActualizaPreferencia()
    {
        var p = new Pago(Guid.NewGuid(), Guid.NewGuid(), 1500m, string.Empty);
        p.SetMpPreferenceId("pref-nuevo");
        Assert.Equal("pref-nuevo", p.MpPreferenceId);
    }
}
