using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Horarios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Horarios;

public class UpdateHorarioCommandTests
{
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificadorInApp> _notificador = new();

    private UpdateHorarioCommand CrearCommand() =>
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

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_ActualizaHorarioYRegistraAuditoria()
    {
        var (_, horario) = CrearHorarioConClase();
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.GetByUnidadYDiaAsync(It.IsAny<Guid>(), It.IsAny<DiaSemana>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(Array.Empty<InscripcionClase>());

        var request = new UpdateHorarioClaseRequest(DiaSemana.Miercoles, "14:00", "15:00", "Sala B");
        var result = await CrearCommand().ExecuteAsync(horario.Id, request, Guid.NewGuid(), "Admin Test");

        Assert.Equal(DiaSemana.Miercoles, result.DiaSemana);
        Assert.Equal("14:00", result.HoraInicio);
        Assert.Equal("15:00", result.HoraFin);
        Assert.Equal("Sala B", result.Sala);

        _horarioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Modificacion, "Horario", horario.Id,
            It.Is<string>(s => s.Contains("Yoga")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioInexistente_LanzaKeyNotFoundException()
    {
        _horarioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((HorarioClase?)null);

        var request = new UpdateHorarioClaseRequest(DiaSemana.Lunes, "09:00", "10:00", null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), request, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_ConHoraInvalida_LanzaArgumentException()
    {
        var (_, horario) = CrearHorarioConClase();
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);

        var request = new UpdateHorarioClaseRequest(DiaSemana.Lunes, "no-valido", "10:00", null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(horario.Id, request, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_ConInscriptos_EnviaNotificaciones()
    {
        var (_, horario) = CrearHorarioConClase();
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.GetByUnidadYDiaAsync(It.IsAny<Guid>(), It.IsAny<DiaSemana>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());

        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Juan",
            apellido: "Perez",
            correo: "juan@test.com",
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

        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { inscripcion });
        _emailService.Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true, null));

        var request = new UpdateHorarioClaseRequest(DiaSemana.Viernes, "16:00", "17:00", null);
        await CrearCommand().ExecuteAsync(horario.Id, request, Guid.NewGuid(), "Admin Test");

        _emailService.Verify(e => e.EnviarAsync("juan@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Modificacion, "Horario", horario.Id,
            It.Is<string>(s => s.Contains("1") && s.Contains("notific")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConInscriptos_CreaNotificacionesInApp()
    {
        var (_, horario) = CrearHorarioConClase();
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.GetByUnidadYDiaAsync(It.IsAny<Guid>(), It.IsAny<DiaSemana>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());

        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Juan",
            apellido: "Perez",
            correo: "juan@test.com",
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

        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { inscripcion });
        _emailService.Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true, null));

        var request = new UpdateHorarioClaseRequest(DiaSemana.Viernes, "16:00", "17:00", null);
        await CrearCommand().ExecuteAsync(horario.Id, request, Guid.NewGuid(), "Admin Test");

        _notificador.Verify(n => n.CrearParaVariosAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Contains(socio.Id)),
            TipoNotificacion.CambioHorario,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SinInscriptos_NoCreaNotificaciones()
    {
        var (_, horario) = CrearHorarioConClase();
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.GetByUnidadYDiaAsync(It.IsAny<Guid>(), It.IsAny<DiaSemana>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());
        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(Array.Empty<InscripcionClase>());

        var request = new UpdateHorarioClaseRequest(DiaSemana.Viernes, "16:00", "17:00", null);
        await CrearCommand().ExecuteAsync(horario.Id, request, Guid.NewGuid(), "Admin Test");

        _notificador.Verify(n => n.CrearParaVariosAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<TipoNotificacion>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NotificadorFalla_NoRompeLaOperacion()
    {
        var (_, horario) = CrearHorarioConClase();
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.GetByUnidadYDiaAsync(It.IsAny<Guid>(), It.IsAny<DiaSemana>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());

        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Juan",
            apellido: "Perez",
            correo: "juan@test.com",
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

        _inscripcionRepo.Setup(r => r.GetActivasByHorarioClaseIdAsync(horario.Id))
            .ReturnsAsync(new[] { inscripcion });
        _emailService.Setup(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true, null));
        _notificador.Setup(n => n.CrearParaVariosAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<TipoNotificacion>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB caida"));

        var request = new UpdateHorarioClaseRequest(DiaSemana.Viernes, "16:00", "17:00", null);
        var result = await CrearCommand().ExecuteAsync(horario.Id, request, Guid.NewGuid(), "Admin Test");

        Assert.Equal(DiaSemana.Viernes, result.DiaSemana);
        _horarioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
