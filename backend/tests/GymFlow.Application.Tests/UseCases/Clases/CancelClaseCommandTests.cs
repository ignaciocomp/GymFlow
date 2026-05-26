using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Clases;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Clases;

public class CancelClaseCommandTests
{
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IEmailService> _emailService = new();

    private CancelClaseCommand CrearCommand() =>
        new(_claseRepo.Object, _auditLogger.Object, _emailService.Object);

    private static Clase CrearClase() =>
        new("Yoga", "Clase de yoga", 20, 60, "Laura García", Guid.NewGuid());

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

    [Fact]
    public async Task ExecuteAsync_ClaseActiva_CancelaYNotificaSocios()
    {
        var clase = CrearClase();
        var socio = CrearSocio();
        var inscripcion = new InscripcionClase(clase.Id, socio.Id);
        // Use reflection to set Socio nav property for email access
        typeof(InscripcionClase).GetProperty("Socio")!.SetValue(inscripcion, socio);

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _claseRepo.Setup(r => r.GetInscripcionesActivasAsync(clase.Id))
            .ReturnsAsync(new[] { inscripcion });
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin Test");

        Assert.False(clase.EstaActivo);
        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Baja, "Clase", clase.Id,
            It.Is<string>(s => s.Contains("1 socios")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ClaseNoExiste_LanzaKeyNotFoundException()
    {
        _claseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Clase?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ClaseYaCancelada_LanzaInvalidOperationException()
    {
        var clase = CrearClase();
        clase.Cancelar(); // ya cancelada

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_EmailFalla_AuditRegistraFallidos()
    {
        var clase = CrearClase();
        var socio = CrearSocio();
        var inscripcion = new InscripcionClase(clase.Id, socio.Id);
        typeof(InscripcionClase).GetProperty("Socio")!.SetValue(inscripcion, socio);

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _claseRepo.Setup(r => r.GetInscripcionesActivasAsync(clase.Id))
            .ReturnsAsync(new[] { inscripcion });
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "SMTP timeout"));

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin Test");

        // Audit log should mention failures
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Baja, "Clase", clase.Id,
            It.Is<string>(s => s.Contains("fallaron")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SinInscripciones_CancelaSinEnviarEmails()
    {
        var clase = CrearClase();
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _claseRepo.Setup(r => r.GetInscripcionesActivasAsync(clase.Id))
            .ReturnsAsync(Array.Empty<InscripcionClase>());
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin");

        Assert.False(clase.EstaActivo);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
