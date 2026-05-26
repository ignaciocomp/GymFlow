using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Horarios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Horarios;

public class CreateHorarioCommandTests
{
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private CreateHorarioCommand CrearCommand() =>
        new(_horarioRepo.Object, _claseRepo.Object, _auditLogger.Object);

    private static Clase CrearClaseActiva()
    {
        var clase = new Clase("Yoga", "Clase de yoga", 20, 60, "Laura García", Guid.NewGuid());
        return clase;
    }

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_CreaHorarioYRegistraAuditoria()
    {
        var clase = CrearClaseActiva();
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _horarioRepo.Setup(r => r.AddAsync(It.IsAny<HorarioClase>())).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _horarioRepo.Setup(r => r.GetByUnidadYDiaAsync(It.IsAny<Guid>(), It.IsAny<DiaSemana>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());

        var request = new CreateHorarioClaseRequest(clase.Id, DiaSemana.Lunes, "09:00", "10:00", "Sala A");
        var result = await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin Test");

        Assert.Equal(DiaSemana.Lunes, result.DiaSemana);
        Assert.Equal("09:00", result.HoraInicio);
        Assert.Equal("10:00", result.HoraFin);
        Assert.Equal("Sala A", result.Sala);

        _horarioRepo.Verify(r => r.AddAsync(It.IsAny<HorarioClase>()), Times.Once);
        _horarioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Creacion, "Horario", It.IsAny<Guid>(),
            It.Is<string>(s => s.Contains("Yoga") && s.Contains("Lunes")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConClaseInexistente_LanzaArgumentException()
    {
        _claseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Clase?)null);

        var request = new CreateHorarioClaseRequest(Guid.NewGuid(), DiaSemana.Lunes, "09:00", "10:00", null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin"));

        _horarioRepo.Verify(r => r.AddAsync(It.IsAny<HorarioClase>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ConClaseCancelada_LanzaInvalidOperationException()
    {
        var clase = CrearClaseActiva();
        clase.Cancelar();
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);

        var request = new CreateHorarioClaseRequest(clase.Id, DiaSemana.Lunes, "09:00", "10:00", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_ConHoraInvalida_LanzaArgumentException()
    {
        var clase = CrearClaseActiva();
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);

        var request = new CreateHorarioClaseRequest(clase.Id, DiaSemana.Lunes, "invalido", "10:00", null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_ConConflictoSala_LanzaInvalidOperationException()
    {
        var clase = CrearClaseActiva();
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);

        var existente = new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
            new TimeOnly(9, 0), new TimeOnly(10, 0), "Sala A");
        _horarioRepo.Setup(r => r.GetByUnidadYDiaAsync(It.IsAny<Guid>(), DiaSemana.Lunes))
            .ReturnsAsync(new[] { existente });

        var request = new CreateHorarioClaseRequest(clase.Id, DiaSemana.Lunes, "09:30", "10:30", "Sala A");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin"));
    }
}
