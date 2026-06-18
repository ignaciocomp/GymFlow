using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class NotificacionTests
{
    private static Notificacion CrearNotificacionValida() =>
        new(Guid.NewGuid(), TipoNotificacion.RecordatorioCuota, "Vencimiento de cuota", "Tu cuota vence en 5 días.");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_ConTituloVacio_Lanza(string? titulo)
    {
        Assert.Throws<ArgumentException>(() =>
            new Notificacion(Guid.NewGuid(), TipoNotificacion.RecordatorioCuota, titulo!, "Mensaje"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_ConMensajeVacio_Lanza(string? mensaje)
    {
        Assert.Throws<ArgumentException>(() =>
            new Notificacion(Guid.NewGuid(), TipoNotificacion.RecordatorioCuota, "Título", mensaje!));
    }

    [Fact]
    public void Ctor_SeteaCamposNoLeidaYFechaCreacion()
    {
        var socioId = Guid.NewGuid();
        var antes = DateTime.UtcNow;

        var notif = new Notificacion(socioId, TipoNotificacion.CambioHorario, "Cambio de horario", "Tu clase cambió de horario.");

        Assert.NotEqual(Guid.Empty, notif.Id);
        Assert.Equal(socioId, notif.SocioId);
        Assert.Equal(TipoNotificacion.CambioHorario, notif.Tipo);
        Assert.Equal("Cambio de horario", notif.Titulo);
        Assert.Equal("Tu clase cambió de horario.", notif.Mensaje);
        Assert.False(notif.Leida);
        Assert.Null(notif.FechaLectura);
        Assert.True(notif.FechaCreacion >= antes && notif.FechaCreacion <= DateTime.UtcNow);
    }

    [Fact]
    public void MarcarLeida_PonenLeidaYFechaLectura()
    {
        var notif = CrearNotificacionValida();
        var ahora = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

        notif.MarcarLeida(ahora);

        Assert.True(notif.Leida);
        Assert.Equal(ahora, notif.FechaLectura);
    }

    [Fact]
    public void MarcarLeida_EsIdempotente_NoCambiaLaFecha()
    {
        var notif = CrearNotificacionValida();
        var primera = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
        var segunda = new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        notif.MarcarLeida(primera);
        notif.MarcarLeida(segunda);

        Assert.True(notif.Leida);
        Assert.Equal(primera, notif.FechaLectura);
    }
}
