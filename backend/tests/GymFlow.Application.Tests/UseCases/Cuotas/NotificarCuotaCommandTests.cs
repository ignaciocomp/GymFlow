using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class NotificarCuotaCommandTests
{
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IRecordatorioCuotaRepository> _recordatorioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private NotificarCuotaCommand CrearCommand() =>
        new(_cuotaRepo.Object, _socioRepo.Object, _recordatorioRepo.Object, _emailService.Object, _auditLogger.Object);

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
    public async Task ExecuteAsync_CuotaPendienteYSocioConCorreo_EnviaEmailYRegistraRecordatorio()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _recordatorioRepo.Setup(r => r.ExisteRecordatorioHoyAsync(cuota.Id, TipoRecordatorio.Manual)).ReturnsAsync(false);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin Test");

        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.Is<string>(a => a.Contains("Plan Musculación")), It.IsAny<string>()), Times.Once);
        _recordatorioRepo.Verify(r => r.AddAsync(It.Is<RecordatorioCuota>(rc =>
            rc.CuotaId == cuota.Id &&
            rc.SocioId == socio.Id &&
            rc.TipoRecordatorio == TipoRecordatorio.Manual &&
            rc.Exitoso == true)), Times.Once);
        _recordatorioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id, It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaNoExiste_LanzaKeyNotFoundException()
    {
        _cuotaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cuota?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaPagada_LanzaInvalidOperationException()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);
        cuota.MarcarComoPagada();

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin"));

        Assert.Contains("pendiente", ex.Message, StringComparison.OrdinalIgnoreCase);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaAnulada_LanzaInvalidOperationException()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);
        cuota.Anular();

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin"));

        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SocioNoExiste_LanzaKeyNotFoundException()
    {
        var cuota = CrearCuotaPendiente(Guid.NewGuid());
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Socio?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin"));
    }

    // NOTA: el caso "socio sin correo" no se puede testear unitariamente porque
    // el constructor de Socio garantiza correo no vacío como invariante del dominio.
    // El check defensivo en NotificarCuotaCommand cubre corrupción de datos eventual.

    [Fact]
    public async Task ExecuteAsync_YaSeNotificoHoy_LanzaInvalidOperationException()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _recordatorioRepo.Setup(r => r.ExisteRecordatorioHoyAsync(cuota.Id, TipoRecordatorio.Manual)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin"));

        Assert.Contains("hoy", ex.Message, StringComparison.OrdinalIgnoreCase);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EmailFalla_RegistraIntentoFallidoYRelanza()
    {
        var socio = CrearSocio();
        var cuota = CrearCuotaPendiente(socio.Id);

        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _recordatorioRepo.Setup(r => r.ExisteRecordatorioHoyAsync(cuota.Id, TipoRecordatorio.Manual)).ReturnsAsync(false);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "SMTP timeout"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin"));

        // Aún registró el intento fallido (para trazabilidad)
        _recordatorioRepo.Verify(r => r.AddAsync(It.Is<RecordatorioCuota>(rc =>
            rc.Exitoso == false && rc.Error == "SMTP timeout")), Times.Once);
        _recordatorioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
