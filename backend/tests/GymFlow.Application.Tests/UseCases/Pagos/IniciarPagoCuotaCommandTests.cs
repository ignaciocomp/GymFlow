using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Pagos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Pagos;

public class IniciarPagoCuotaCommandTests
{
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<IPagoRepository> _pagoRepo = new();
    private readonly Mock<IMercadoPagoService> _mpService = new();
    private readonly Mock<IPagoUrlBuilder> _urlBuilder = new();

    public IniciarPagoCuotaCommandTests()
    {
        _urlBuilder.Setup(u => u.BuildBackUrls())
            .Returns(new BackUrls("https://front/success", "https://front/failure", "https://front/pending"));
        _urlBuilder.Setup(u => u.BuildNotificationUrl())
            .Returns("https://api/api/pagos/webhook");
    }

    private IniciarPagoCuotaCommand CrearCommand() =>
        new(_cuotaRepo.Object, _pagoRepo.Object, _mpService.Object, _urlBuilder.Object);

    private static Cuota CrearCuota(Guid socioId) =>
        new(socioId, Guid.NewGuid(), Guid.NewGuid(), "Plan Musculación", 2500m, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_CuotaPendienteDelSocio_CreaPagoYDevuelveInitPoint()
    {
        var socioId = Guid.NewGuid();
        var cuota = CrearCuota(socioId);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _mpService.Setup(s => s.CrearPreferenciaAsync(
                It.IsAny<Guid>(), cuota.Monto, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BackUrls>()))
            .ReturnsAsync(new PreferenciaResultado("pref-123", "https://mp/init"));

        var resultado = await CrearCommand().ExecuteAsync(cuota.Id, socioId);

        Assert.Equal("https://mp/init", resultado.InitPoint);
        _pagoRepo.Verify(r => r.AddAsync(It.Is<Pago>(p =>
            p.CuotaId == cuota.Id && p.SocioId == socioId && p.Monto == cuota.Monto)), Times.Once);
        // Dos SaveChanges: uno para obtener el pago.Id, otro tras guardar el MpPreferenceId.
        _pagoRepo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        _mpService.Verify(s => s.CrearPreferenciaAsync(
            It.IsAny<Guid>(), cuota.Monto, It.IsAny<string>(), "https://api/api/pagos/webhook", It.IsAny<BackUrls>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ExternalReferenceEsElPagoId()
    {
        var socioId = Guid.NewGuid();
        var cuota = CrearCuota(socioId);
        Guid capturedPagoId = Guid.Empty;
        Guid externalRef = Guid.Empty;
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _pagoRepo.Setup(r => r.AddAsync(It.IsAny<Pago>())).Callback<Pago>(p => capturedPagoId = p.Id);
        _mpService.Setup(s => s.CrearPreferenciaAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BackUrls>()))
            .Callback<Guid, decimal, string, string, BackUrls>((id, _, _, _, _) => externalRef = id)
            .ReturnsAsync(new PreferenciaResultado("pref-123", "https://mp/init"));

        await CrearCommand().ExecuteAsync(cuota.Id, socioId);

        Assert.Equal(capturedPagoId, externalRef);
        Assert.NotEqual(Guid.Empty, externalRef);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaNoExiste_ThrowsKeyNotFound()
    {
        _cuotaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cuota?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid()));

        _pagoRepo.Verify(r => r.AddAsync(It.IsAny<Pago>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaDeOtroSocio_ThrowsYNoCreaPago()
    {
        var cuota = CrearCuota(Guid.NewGuid()); // socio dueño distinto
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid()));

        _pagoRepo.Verify(r => r.AddAsync(It.IsAny<Pago>()), Times.Never);
        _mpService.Verify(s => s.CrearPreferenciaAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BackUrls>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaYaPagada_ThrowsInvalidOperation()
    {
        var socioId = Guid.NewGuid();
        var cuota = CrearCuota(socioId);
        cuota.MarcarComoPagada();
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, socioId));

        _pagoRepo.Verify(r => r.AddAsync(It.IsAny<Pago>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CrearPreferenciaFalla_PropagaError()
    {
        var socioId = Guid.NewGuid();
        var cuota = CrearCuota(socioId);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _mpService.Setup(s => s.CrearPreferenciaAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BackUrls>()))
            .ThrowsAsync(new HttpRequestException("MP caído"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, socioId));
    }
}
