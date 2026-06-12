using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class MarcarCuotaPagadaCommandTests
{
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private MarcarCuotaPagadaCommand CrearCommand() =>
        new(_cuotaRepo.Object, _socioRepo.Object, _emailService.Object, _auditLogger.Object);

    private static Socio CrearSocio(string correo = "socio@test.com") =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: "María",
            apellido: "López",
            correo: correo,
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);

    private static Cuota CrearCuotaPendiente(Guid socioId) =>
        new(socioId, Guid.NewGuid(), Guid.NewGuid(), "Plan Musculación", 2500m, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_CuotaExiste_MarcaComoPagadaYAudita()
    {
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        await CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin Test");

        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
        _cuotaRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id, It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaNoExiste_ThrowsKeyNotFoundException()
    {
        _cuotaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cuota?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PagoExitoso_EnviaEmailDeConfirmacion()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);
        var periodo = cuota.FechaEmision.ToString("MM/yyyy");

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin Test");

        // El asunto confirma el pago y el cuerpo lleva los datos de la cuota.
        // "Musculación" queda HTML-encoded ("Musculaci&#243;n"), por eso se chequea el prefijo.
        _emailService.Verify(s => s.EnviarAsync(
            socio.Correo,
            It.Is<string>(a => a.Contains("Pago confirmado")),
            It.Is<string>(c => c.Contains("Musculaci") && c.Contains(periodo))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SiEmailFalla_ElPagoIgualSeConfirma()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "smtp down"));

        // Best-effort: el fallo del envío NO debe propagar excepción.
        await CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin");

        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
        _cuotaRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id, It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SiEmailLanzaExcepcion_ElPagoIgualSeConfirma()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        // Incluso si el servicio de email lanza, el pago queda registrado.
        await CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin");

        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
        _cuotaRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
