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
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private InscribirSocioCommand CrearCommand() =>
        new(_inscripcionRepo.Object, _horarioRepo.Object, _cuotaRepo.Object,
            _socioRepo.Object, _emailService.Object, _auditLogger.Object);

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
    public async Task ClaseCancelada_LanzaInvalidOperation()
    {
        var clase = CrearClase(capacidad: 10);
        clase.Cancelar();
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin"));

        Assert.Equal("No se puede inscribir a una clase cancelada.", ex.Message);
        _inscripcionRepo.Verify(r => r.AddAsync(It.IsAny<InscripcionClase>()), Times.Never);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task YaInscriptoEnHorario_LanzaInvalidOperation()
    {
        var clase = CrearClase(capacidad: 10);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id))
            .ReturnsAsync(new InscripcionClase(horario.Id, socio.Id));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin"));

        Assert.Equal("Ya estas inscripto en este horario.", ex.Message);
        _inscripcionRepo.Verify(r => r.AddAsync(It.IsAny<InscripcionClase>()), Times.Never);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConCuotaVencidaEnUnidad_LanzaInvalidOperationYNoInscribe()
    {
        var clase = CrearClase(capacidad: 10);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(socio.Id, clase.UnidadId)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin"));

        Assert.Equal("No podés inscribirte con cuota vencida en esta sede.", ex.Message);
        _inscripcionRepo.Verify(r => r.AddAsync(It.IsAny<InscripcionClase>()), Times.Never);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConCuotaAlDia_Inscribe()
    {
        var clase = CrearClase(capacidad: 10);
        var horario = CrearHorario(clase);
        var socio = CrearSocio();

        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(socio.Id, clase.UnidadId)).ReturnsAsync(false);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYHorarioAsync(socio.Id, horario.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(horario.Id)).ReturnsAsync(2);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var dto = await CrearCommand().ExecuteAsync(socio.Id, horario.Id, Guid.NewGuid(), "Admin");

        Assert.Equal(horario.Id, dto.HorarioClaseId);
        _cuotaRepo.Verify(r => r.TieneCuotasVencidasEnUnidadAsync(socio.Id, clase.UnidadId), Times.Once);
        _inscripcionRepo.Verify(r => r.AddAsync(It.Is<InscripcionClase>(i =>
            i.HorarioClaseId == horario.Id)), Times.Once);
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
}
