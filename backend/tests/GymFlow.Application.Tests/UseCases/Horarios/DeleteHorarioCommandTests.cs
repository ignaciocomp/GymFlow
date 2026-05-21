using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Horarios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Horarios;

public class DeleteHorarioCommandTests
{
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private DeleteHorarioCommand CrearCommand() =>
        new(_horarioRepo.Object, _auditLogger.Object);

    [Fact]
    public async Task ExecuteAsync_ConHorarioExistente_EliminaYRegistraAuditoria()
    {
        var horario = new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
            new TimeOnly(9, 0), new TimeOnly(10, 0), "Sala A");
        _horarioRepo.Setup(r => r.GetByIdAsync(horario.Id)).ReturnsAsync(horario);
        _horarioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await CrearCommand().ExecuteAsync(horario.Id, Guid.NewGuid(), "Admin Test");

        _horarioRepo.Verify(r => r.Remove(horario), Times.Once);
        _horarioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Baja, "Horario", horario.Id,
            It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioInexistente_LanzaKeyNotFoundException()
    {
        _horarioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((HorarioClase?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _horarioRepo.Verify(r => r.Remove(It.IsAny<HorarioClase>()), Times.Never);
    }
}
