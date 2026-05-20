using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Clases;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Clases;

public class ReactivarClaseCommandTests
{
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private ReactivarClaseCommand CrearCommand() =>
        new(_claseRepo.Object, _auditLogger.Object);

    private static Clase CrearClaseCancelada()
    {
        var clase = new Clase("Yoga", "Clase de yoga", 20, 60, "Laura García", Guid.NewGuid());
        clase.Cancelar();
        return clase;
    }

    [Fact]
    public async Task ExecuteAsync_ClaseCancelada_ReactivaYRegistraAuditoria()
    {
        var clase = CrearClaseCancelada();
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin Test");

        Assert.True(result.EstaActivo);
        Assert.Equal("Yoga", result.Nombre);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Reactivacion, "Clase", clase.Id,
            It.Is<string>(s => s.Contains("Yoga")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ClaseNoExiste_LanzaKeyNotFoundException()
    {
        _claseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Clase?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_ClaseYaActiva_LanzaInvalidOperationException()
    {
        var clase = new Clase("Yoga", "Desc", 20, 60, "Instructor", Guid.NewGuid());
        // clase ya está activa
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(clase.Id, Guid.NewGuid(), "Admin"));
    }
}
