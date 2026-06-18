using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Notificaciones;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Notificaciones;

public class NotificacionesPortalTests
{
    private readonly Mock<INotificacionRepository> _repo = new();

    private static Notificacion Notif(Guid socioId, TipoNotificacion tipo, string titulo, string mensaje) =>
        new(socioId, tipo, titulo, mensaje);

    // ---------- GetNotificacionesQuery ----------

    [Fact]
    public async Task GetNotificaciones_DevuelveDelSocioComoDto()
    {
        var socioId = Guid.NewGuid();
        var n1 = Notif(socioId, TipoNotificacion.RecordatorioCuota, "Cuota", "Tu cuota vence pronto.");
        var n2 = Notif(socioId, TipoNotificacion.EventoNuevo, "Evento", "Hay un evento nuevo.");
        _repo.Setup(r => r.GetBySocioAsync(socioId, false, 20)).ReturnsAsync(new[] { n1, n2 });

        var sut = new GetNotificacionesQuery(_repo.Object);
        var result = (await sut.ExecuteAsync(socioId, soloNoLeidas: false, take: 20)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(n1.Id, result[0].Id);
        Assert.Equal(TipoNotificacion.RecordatorioCuota, result[0].Tipo);
        Assert.Equal("Cuota", result[0].Titulo);
        Assert.Equal("Tu cuota vence pronto.", result[0].Mensaje);
        Assert.False(result[0].Leida);
        Assert.Equal(n1.FechaCreacion, result[0].FechaCreacion);
    }

    [Fact]
    public async Task GetNotificaciones_ClampeaTakeExcesivo()
    {
        var socioId = Guid.NewGuid();
        _repo.Setup(r => r.GetBySocioAsync(socioId, false, 100)).ReturnsAsync(Array.Empty<Notificacion>());

        var sut = new GetNotificacionesQuery(_repo.Object);
        await sut.ExecuteAsync(socioId, soloNoLeidas: false, take: 1_000_000);

        // Un take desmedido se acota a 100 (evita pedir cargas enormes).
        _repo.Verify(r => r.GetBySocioAsync(socioId, false, 100), Times.Once);
    }

    [Fact]
    public async Task GetNotificaciones_TakeNoPositivo_SeAcotaA1()
    {
        var socioId = Guid.NewGuid();
        _repo.Setup(r => r.GetBySocioAsync(socioId, false, 1)).ReturnsAsync(Array.Empty<Notificacion>());

        var sut = new GetNotificacionesQuery(_repo.Object);
        await sut.ExecuteAsync(socioId, soloNoLeidas: false, take: 0);

        _repo.Verify(r => r.GetBySocioAsync(socioId, false, 1), Times.Once);
    }

    [Fact]
    public async Task GetNotificaciones_PasaSoloNoLeidasYTakeAlRepo()
    {
        var socioId = Guid.NewGuid();
        _repo.Setup(r => r.GetBySocioAsync(socioId, true, 5)).ReturnsAsync(Array.Empty<Notificacion>());

        var sut = new GetNotificacionesQuery(_repo.Object);
        var result = (await sut.ExecuteAsync(socioId, soloNoLeidas: true, take: 5)).ToList();

        Assert.Empty(result);
        _repo.Verify(r => r.GetBySocioAsync(socioId, true, 5), Times.Once);
    }

    // ---------- ContarNoLeidasQuery ----------

    [Fact]
    public async Task ContarNoLeidas_DevuelveElConteoDelRepo()
    {
        var socioId = Guid.NewGuid();
        _repo.Setup(r => r.ContarNoLeidasAsync(socioId)).ReturnsAsync(3);

        var sut = new ContarNoLeidasQuery(_repo.Object);
        var count = await sut.ExecuteAsync(socioId);

        Assert.Equal(3, count);
    }

    // ---------- MarcarNotificacionLeidaCommand ----------

    [Fact]
    public async Task MarcarLeida_DeOtroSocio_Lanza()
    {
        var socioId = Guid.NewGuid();
        var otroSocioId = Guid.NewGuid();
        var notif = Notif(otroSocioId, TipoNotificacion.RecordatorioCuota, "Cuota", "Mensaje");
        _repo.Setup(r => r.GetByIdAsync(notif.Id)).ReturnsAsync(notif);

        var sut = new MarcarNotificacionLeidaCommand(_repo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ExecuteAsync(notif.Id, socioId));
        Assert.False(notif.Leida);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarcarLeida_NoExiste_Lanza()
    {
        var socioId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Notificacion?)null);

        var sut = new MarcarNotificacionLeidaCommand(_repo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ExecuteAsync(id, socioId));
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarcarLeida_Propia_MarcaYGuarda()
    {
        var socioId = Guid.NewGuid();
        var notif = Notif(socioId, TipoNotificacion.RecordatorioCuota, "Cuota", "Mensaje");
        _repo.Setup(r => r.GetByIdAsync(notif.Id)).ReturnsAsync(notif);

        var sut = new MarcarNotificacionLeidaCommand(_repo.Object);
        await sut.ExecuteAsync(notif.Id, socioId);

        Assert.True(notif.Leida);
        Assert.NotNull(notif.FechaLectura);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarcarLeida_Idempotente_NoCambiaLaFecha()
    {
        var socioId = Guid.NewGuid();
        var notif = Notif(socioId, TipoNotificacion.RecordatorioCuota, "Cuota", "Mensaje");
        var primera = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
        notif.MarcarLeida(primera);
        _repo.Setup(r => r.GetByIdAsync(notif.Id)).ReturnsAsync(notif);

        var sut = new MarcarNotificacionLeidaCommand(_repo.Object);
        await sut.ExecuteAsync(notif.Id, socioId);

        Assert.True(notif.Leida);
        Assert.Equal(primera, notif.FechaLectura);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ---------- MarcarTodasLeidasCommand ----------

    [Fact]
    public async Task MarcarTodasLeidas_DelegaEnRepoYGuarda()
    {
        var socioId = Guid.NewGuid();

        var sut = new MarcarTodasLeidasCommand(_repo.Object);
        await sut.ExecuteAsync(socioId);

        _repo.Verify(r => r.MarcarTodasLeidasAsync(socioId, It.IsAny<DateTime>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
