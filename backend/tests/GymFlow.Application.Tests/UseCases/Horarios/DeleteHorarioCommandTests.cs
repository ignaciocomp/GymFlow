using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Horarios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Horarios;

public class DeleteHorarioCommandTests
{
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificadorInApp> _notificador = new();

    private DeleteHorarioCommand CrearCommand() =>
        new(_horarioRepo.Object, _inscripcionRepo.Object, _auditLogger.Object,
            _emailService.Object, _notificador.Object);

    private static (Clase clase, HorarioClase horario) CrearHorarioConClase()
    {
        var unidad = new Unidad("Espacio Mora", "Av. 8 de Octubre 2845");
        var clase = new Clase("Yoga", "Clase de yoga", 20, 60, "Laura Garcia", unidad.Id);
        var horario = new HorarioClase(clase.Id, DiaSemana.Lunes,
            new TimeOnly(9, 0), new TimeOnly(10, 0), "Sala A");
        typeof(HorarioClase).GetProperty("Clase")!.SetValue(horario, clase);
        typeof(Clase).GetProperty("Unidad")!.SetValue(clase, unidad);
        return (clase, horario);
    }

    private static InscripcionClase CrearInscripcion(HorarioClase horario, string correo)
    {
        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Juan",
            apellido: "Perez",
            correo: correo,
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);
        var inscripcion = new InscripcionClase(horario.Id, socio.Id);
        typeof(InscripcionClase).GetProperty("Socio")!.SetValue(inscripcion, socio);
        typeof(InscripcionClase).GetProperty("HorarioClase")!.SetValue(inscripcion, horario);
        return inscripcion;
    }

    [Fact]
    public async Task SinInscriptos_EliminaAuditaYNoNotifica()
    {
        var (_, horario) = CrearHorarioConClase();
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(Array.Empty<InscripcionClase>());

        await CrearCommand().ExecuteAsync(horario.Id, Guid.NewGuid(), "Admin Test");

        _horarioRepo.Verify(r => r.Remove(horario), Times.Once);
        _horarioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _notificador.Verify(n => n.CrearParaVariosAsync(It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<TipoNotificacion>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Baja, "Horario", horario.Id,
            It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ConHorarioInexistente_LanzaKeyNotFoundException()
    {
        _horarioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((HorarioClase?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _horarioRepo.Verify(r => r.Remove(It.IsAny<HorarioClase>()), Times.Never);
    }

    [Fact]
    public async Task ConInscriptos_CancelaLasInscripcionesYLasPersisteAntesDeBorrar()
    {
        // E2E-05: el borrado de un horario con inscriptos no puede dejar inscripciones
        // huérfanas por la cascade: primero se cancelan (soft) y se persisten.
        var (_, horario) = CrearHorarioConClase();
        var insc1 = CrearInscripcion(horario, "s1@test.com");
        var insc2 = CrearInscripcion(horario, "s2@test.com");

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { insc1, insc2 });
        _emailService.Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true, null));

        bool cancelacionesPersistidasAntesDelRemove = false;
        _inscripcionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.Remove(horario))
            .Callback(() => cancelacionesPersistidasAntesDelRemove =
                _inscripcionRepo.Invocations.Any(i => i.Method.Name == nameof(IInscripcionClaseRepository.SaveChangesAsync)));

        await CrearCommand().ExecuteAsync(horario.Id, Guid.NewGuid(), "Admin Test");

        Assert.False(insc1.EstaActiva);
        Assert.False(insc2.EstaActiva);
        _inscripcionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _horarioRepo.Verify(r => r.Remove(horario), Times.Once);
        _horarioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        Assert.True(cancelacionesPersistidasAntesDelRemove);
    }

    [Fact]
    public async Task ConInscriptos_EnviaEmailYCreaNotificacionInAppACadaSocio()
    {
        var (_, horario) = CrearHorarioConClase();
        var insc1 = CrearInscripcion(horario, "s1@test.com");
        var insc2 = CrearInscripcion(horario, "s2@test.com");

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { insc1, insc2 });
        _emailService.Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true, null));

        await CrearCommand().ExecuteAsync(horario.Id, Guid.NewGuid(), "Admin Test");

        _emailService.Verify(e => e.EnviarAsync("s1@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _emailService.Verify(e => e.EnviarAsync("s2@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _notificador.Verify(n => n.CrearParaVariosAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Contains(insc1.SocioId) && ids.Contains(insc2.SocioId)),
            TipoNotificacion.CancelacionClase,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConInscriptos_AuditaElResultadoDelEnvio()
    {
        var (_, horario) = CrearHorarioConClase();
        var insc1 = CrearInscripcion(horario, "s1@test.com");
        var insc2 = CrearInscripcion(horario, "s2@test.com");

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { insc1, insc2 });
        _emailService.SetupSequence(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true, null))
            .ReturnsAsync(new EmailResultado(false, "SMTP timeout"));

        await CrearCommand().ExecuteAsync(horario.Id, Guid.NewGuid(), "Admin Test");

        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Baja, "Horario", horario.Id,
            It.Is<string>(s => s.Contains("1 de 2") && s.Contains("fallaron")), null), Times.Once);
    }

    [Fact]
    public async Task NotificadorFalla_ElBorradoIgualSeCompleta()
    {
        var (_, horario) = CrearHorarioConClase();
        var insc = CrearInscripcion(horario, "s1@test.com");

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { insc });
        _emailService.Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true, null));
        _notificador.Setup(n => n.CrearParaVariosAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<TipoNotificacion>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB caida"));

        await CrearCommand().ExecuteAsync(horario.Id, Guid.NewGuid(), "Admin Test");

        _horarioRepo.Verify(r => r.Remove(horario), Times.Once);
        _horarioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
