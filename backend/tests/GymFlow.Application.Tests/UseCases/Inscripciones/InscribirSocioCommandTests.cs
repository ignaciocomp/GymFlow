using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Inscripciones;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Inscripciones;

public class InscribirSocioCommandTests
{
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<INotificadorInApp> _notificador = new();

    private InscribirSocioCommand CrearCommand() =>
        new(_inscripcionRepo.Object, _horarioRepo.Object,
            _socioRepo.Object, _emailService.Object, _auditLogger.Object, _notificador.Object);

    private static Socio CrearSocio() =>
        new(Guid.NewGuid(), "Maria", "Lopez", "m@test.com", "h", DateTime.UtcNow,
            true, TipoDocumento.CI, null, "12345672", null);

    private static Clase CrearClase(int capacidad = 10) =>
        new("Spinning", "Clase de spinning", capacidad, 60, "Juan", Guid.NewGuid());

    private static HorarioClase CrearHorario(Clase clase)
    {
        var horario = new HorarioClase(clase.Id, DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(9, 0), "Sala 1");
        typeof(HorarioClase).GetProperty("Clase")!.SetValue(horario, clase);
        return horario;
    }

    [Fact]
    public async Task ConCupo_InscribeNormalEnviaEmailYAudita()
    {
        var clase = CrearClase(capacidad: 10);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(horario.Id)).ReturnsAsync(3);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var dto = await CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin");

        Assert.Equal(horario.Id, dto.HorarioClaseId);
        _inscripcionRepo.Verify(r => r.AddAsync(It.Is<InscripcionClase>(i =>
            i.HorarioClaseId == horario.Id)), Times.Once);
        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin",
            TipoAccionAuditoria.Creacion, "Inscripcion", It.IsAny<Guid?>(), It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task SinCupo_LanzaInvalidOperation()
    {
        var clase = CrearClase(capacidad: 5);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(horario.Id)).ReturnsAsync(5);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin"));

        Assert.Equal("No hay cupos disponibles para este horario.", ex.Message);
        _inscripcionRepo.Verify(r => r.AddAsync(It.IsAny<InscripcionClase>()), Times.Never);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConCupo_CreaNotificacionConfirmacionInApp()
    {
        var clase = CrearClase(capacidad: 10);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(horario.Id)).ReturnsAsync(3);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin");

        _notificador.Verify(n => n.CrearAsync(
            socio.Id, TipoNotificacion.ConfirmacionInscripcion,
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConCupo_NotificadorLanza_LaInscripcionIgualSeConfirma()
    {
        // Best-effort: si crear la notificación in-app falla, la inscripción
        // ya guardada igual se confirma (no se relanza la excepción).
        var clase = CrearClase(capacidad: 10);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(horario.Id)).ReturnsAsync(3);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));
        _notificador.Setup(n => n.CrearAsync(It.IsAny<Guid>(), It.IsAny<TipoNotificacion>(),
            It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("DB caída"));

        var dto = await CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin");

        Assert.Equal(horario.Id, dto.HorarioClaseId);
        _inscripcionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
