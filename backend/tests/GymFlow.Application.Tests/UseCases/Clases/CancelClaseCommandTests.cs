using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Clases;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Clases;

public class CancelClaseCommandTests
{
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificadorInApp> _notificador = new();

    private CancelClaseCommand CrearCommand() =>
        new(_claseRepo.Object, _horarioRepo.Object, _inscripcionRepo.Object,
            _auditLogger.Object, _emailService.Object, _notificador.Object);

    private static Clase CrearClase() =>
        new("Yoga", "Clase de yoga", 20, 60, "Laura Garcia", Guid.NewGuid());

    private static HorarioClase CrearHorario(Clase clase)
    {
        var horario = new HorarioClase(clase.Id, DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(9, 0), "Sala 1");
        typeof(HorarioClase).GetProperty("Clase")!.SetValue(horario, clase);
        return horario;
    }

    private static Socio CrearSocio(string correo = "socio@test.com") =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: "Maria",
            apellido: "Lopez",
            correo: correo,
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);

    private static InscripcionClase CrearInscripcion(HorarioClase horario, Socio socio)
    {
        var inscripcion = new InscripcionClase(horario.Id, socio.Id);
        typeof(InscripcionClase).GetProperty("Socio")!.SetValue(inscripcion, socio);
        typeof(InscripcionClase).GetProperty("HorarioClase")!.SetValue(inscripcion, horario);
        return inscripcion;
    }

    [Fact]
    public async Task ExecuteAsync_ClaseActiva_CancelaYNotificaSocios()
    {
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        var socio = CrearSocio();
        var inscripcion = CrearInscripcion(horario, socio);

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _horarioRepo.Setup(r => r.GetByClaseIdAsync(clase.Id)).ReturnsAsync(new[] { horario });
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
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
        clase.Cancelar();

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_EmailFalla_AuditRegistraFallidos()
    {
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        var socio = CrearSocio();
        var inscripcion = CrearInscripcion(horario, socio);

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _horarioRepo.Setup(r => r.GetByClaseIdAsync(clase.Id)).ReturnsAsync(new[] { horario });
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { inscripcion });
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "SMTP timeout"));

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin Test");

        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Baja, "Clase", clase.Id,
            It.Is<string>(s => s.Contains("fallaron")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SinInscripciones_CancelaSinEnviarEmails()
    {
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _horarioRepo.Setup(r => r.GetByClaseIdAsync(clase.Id)).ReturnsAsync(new[] { horario });
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(Array.Empty<InscripcionClase>());
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin");

        Assert.False(clase.EstaActivo);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ConInscriptos_CreaNotificacionesInApp()
    {
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        var socio = CrearSocio();
        var inscripcion = CrearInscripcion(horario, socio);

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _horarioRepo.Setup(r => r.GetByClaseIdAsync(clase.Id)).ReturnsAsync(new[] { horario });
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { inscripcion });
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin Test");

        _notificador.Verify(n => n.CrearParaVariosAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Contains(socio.Id)),
            TipoNotificacion.CancelacionClase,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SinInscriptos_NoCreaNotificaciones()
    {
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _horarioRepo.Setup(r => r.GetByClaseIdAsync(clase.Id)).ReturnsAsync(new[] { horario });
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(Array.Empty<InscripcionClase>());
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin");

        _notificador.Verify(n => n.CrearParaVariosAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<TipoNotificacion>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NotificadorFalla_NoRompeLaCancelacion()
    {
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        var socio = CrearSocio();
        var inscripcion = CrearInscripcion(horario, socio);

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _horarioRepo.Setup(r => r.GetByClaseIdAsync(clase.Id)).ReturnsAsync(new[] { horario });
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { inscripcion });
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));
        _notificador.Setup(n => n.CrearParaVariosAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<TipoNotificacion>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB caida"));

        await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin Test");

        Assert.False(clase.EstaActivo);
        _claseRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
