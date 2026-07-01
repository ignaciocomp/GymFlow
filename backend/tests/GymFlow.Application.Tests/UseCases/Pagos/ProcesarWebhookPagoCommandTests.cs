using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Pagos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Pagos;

public class ProcesarWebhookPagoCommandTests
{
    private readonly Mock<IMercadoPagoService> _mp = new();
    private readonly Mock<IPagoRepository> _pagoRepo = new();
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private ProcesarWebhookPagoCommand CrearCommand() =>
        new(_mp.Object, _pagoRepo.Object, _cuotaRepo.Object, _socioRepo.Object, _emailService.Object, _auditLogger.Object);

    private static Socio CrearSocio(string correo = "socio@test.com") =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: "María", apellido: "López", correo: correo, passwordHash: "hash",
            fechaAlta: DateTime.UtcNow, consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI, telefono: null,
            documentoIdentidad: "12345672", fechaNacimiento: null);

    private static Cuota CrearCuota(Guid socioId) =>
        new(socioId, Guid.NewGuid(), Guid.NewGuid(), "Plan Musculación", 2500m, DateTime.UtcNow);

    private void SetupFirmaValida(bool valida = true) =>
        _mp.Setup(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>())).Returns(valida);

    // --- Firma inválida (CA-36) ---

    [Fact]
    public async Task ExecuteAsync_FirmaInvalida_NoConsultaMpNiTocaDatos_AuditaYRetornaFirmaInvalida()
    {
        SetupFirmaValida(false);

        var resultado = await CrearCommand().ExecuteAsync("123", "sig", "req");

        Assert.Equal(WebhookResultado.FirmaInvalida, resultado);
        _mp.Verify(m => m.ObtenerPagoAsync(It.IsAny<string>()), Times.Never);
        _pagoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _cuotaRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _auditLogger.Verify(a => a.LogAsync(
            Guid.Empty, "Sistema", It.IsAny<TipoAccionAuditoria>(),
            It.IsAny<string>(), It.IsAny<Guid?>(),
            It.Is<string>(d => d.Contains("firma inválida")), It.IsAny<string?>()), Times.Once);
    }

    // --- Approved (CA-34/35) ---

    [Fact]
    public async Task ExecuteAsync_Approved_MarcaCuotaPagadaYPagoAprobado_EnviaEmailYAudita()
    {
        SetupFirmaValida();
        var socio = CrearSocio();
        var cuota = CrearCuota(socio.Id);
        var pago = new Pago(cuota.Id, socio.Id, cuota.Monto, "pref-1");
        _mp.Setup(m => m.ObtenerPagoAsync("999"))
            .ReturnsAsync(new PagoMpInfo("approved", "credit_card", pago.Id.ToString(), "mp-999"));
        _pagoRepo.Setup(r => r.GetByExternalReferenceAsync(pago.Id)).ReturnsAsync(pago);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true));

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Procesado, resultado);
        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
        Assert.Equal(EstadoPago.Aprobado, pago.Estado);
        Assert.Equal("mp-999", pago.MpPaymentId);
        Assert.Equal("credit_card", pago.MedioPago);
        _emailService.Verify(s => s.EnviarAsync(
            socio.Correo, It.Is<string>(a => a.Contains("Pago confirmado")), It.IsAny<string>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), TipoAccionAuditoria.Modificacion,
            "Cuota", cuota.Id, It.Is<string>(d => d.Contains("Mercado Pago")), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Approved_HaceUnSoloSaveChanges()
    {
        SetupFirmaValida();
        var socio = CrearSocio();
        var cuota = CrearCuota(socio.Id);
        var pago = new Pago(cuota.Id, socio.Id, cuota.Monto, "pref-1");
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>()))
            .ReturnsAsync(new PagoMpInfo("approved", "credit_card", pago.Id.ToString(), "mp-999"));
        _pagoRepo.Setup(r => r.GetByExternalReferenceAsync(pago.Id)).ReturnsAsync(pago);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true));

        await CrearCommand().ExecuteAsync("999", "sig", "req");

        // DbContext compartido: un solo SaveChanges commitea Cuota + Pago atómicamente.
        _pagoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // --- Idempotencia (E4) ---

    [Fact]
    public async Task ExecuteAsync_ApprovedPeroCuotaYaPagada_Idempotente_NoReenviaEmail()
    {
        SetupFirmaValida();
        var socio = CrearSocio();
        var cuota = CrearCuota(socio.Id);
        cuota.MarcarComoPagada();
        var pago = new Pago(cuota.Id, socio.Id, cuota.Monto, "pref-1");
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>()))
            .ReturnsAsync(new PagoMpInfo("approved", "credit_card", pago.Id.ToString(), "mp-999"));
        _pagoRepo.Setup(r => r.GetByExternalReferenceAsync(pago.Id)).ReturnsAsync(pago);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Ignorado, resultado);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _pagoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // --- Rejected (E1/CA-37) ---

    [Fact]
    public async Task ExecuteAsync_Rejected_MarcaPagoRechazado_CuotaSinCambios()
    {
        SetupFirmaValida();
        var socio = CrearSocio();
        var cuota = CrearCuota(socio.Id);
        var pago = new Pago(cuota.Id, socio.Id, cuota.Monto, "pref-1");
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>()))
            .ReturnsAsync(new PagoMpInfo("rejected", null, pago.Id.ToString(), "mp-999"));
        _pagoRepo.Setup(r => r.GetByExternalReferenceAsync(pago.Id)).ReturnsAsync(pago);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Procesado, resultado);
        Assert.Equal(EstadoPago.Rechazado, pago.Estado);
        Assert.Equal(EstadoCuota.Pendiente, cuota.Estado);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _pagoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // --- Ignorados ---

    [Fact]
    public async Task ExecuteAsync_PagoNoEncontradoEnMp_RetornaIgnorado()
    {
        SetupFirmaValida();
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>())).ReturnsAsync((PagoMpInfo?)null);

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Ignorado, resultado);
        _pagoRepo.Verify(r => r.GetByExternalReferenceAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExternalReferenceNoEsGuid_RetornaIgnorado()
    {
        SetupFirmaValida();
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>()))
            .ReturnsAsync(new PagoMpInfo("approved", "credit_card", "no-es-guid", "mp-999"));

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Ignorado, resultado);
        _pagoRepo.Verify(r => r.GetByExternalReferenceAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PagoNoEnBd_RetornaIgnorado()
    {
        SetupFirmaValida();
        var pagoId = Guid.NewGuid();
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>()))
            .ReturnsAsync(new PagoMpInfo("approved", "credit_card", pagoId.ToString(), "mp-999"));
        _pagoRepo.Setup(r => r.GetByExternalReferenceAsync(pagoId)).ReturnsAsync((Pago?)null);

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Ignorado, resultado);
    }

    [Fact]
    public async Task ExecuteAsync_EstadoPending_RetornaIgnoradoSinCambios()
    {
        SetupFirmaValida();
        var socio = CrearSocio();
        var cuota = CrearCuota(socio.Id);
        var pago = new Pago(cuota.Id, socio.Id, cuota.Monto, "pref-1");
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>()))
            .ReturnsAsync(new PagoMpInfo("pending", "ticket", pago.Id.ToString(), "mp-999"));
        _pagoRepo.Setup(r => r.GetByExternalReferenceAsync(pago.Id)).ReturnsAsync(pago);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Ignorado, resultado);
        Assert.Equal(EstadoPago.Pendiente, pago.Estado);
        Assert.Equal(EstadoCuota.Pendiente, cuota.Estado);
        _pagoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Approved_SiEmailFalla_ElPagoIgualSeProcesa()
    {
        SetupFirmaValida();
        var socio = CrearSocio();
        var cuota = CrearCuota(socio.Id);
        var pago = new Pago(cuota.Id, socio.Id, cuota.Monto, "pref-1");
        _mp.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>()))
            .ReturnsAsync(new PagoMpInfo("approved", "credit_card", pago.Id.ToString(), "mp-999"));
        _pagoRepo.Setup(r => r.GetByExternalReferenceAsync(pago.Id)).ReturnsAsync(pago);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var resultado = await CrearCommand().ExecuteAsync("999", "sig", "req");

        Assert.Equal(WebhookResultado.Procesado, resultado);
        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
    }
}
