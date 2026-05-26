using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Clases;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Clases;

public class UpdateClaseCommandTests
{
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private UpdateClaseCommand CrearCommand() =>
        new(_claseRepo.Object, _auditLogger.Object);

    private static Clase CrearClase() =>
        new("Yoga", "Clase de yoga", 20, 60, "Laura García", Guid.NewGuid());

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_ActualizaClaseYRegistraAuditoria()
    {
        var clase = CrearClase();
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _claseRepo.Setup(r => r.GetInscripcionesActivasCountAsync(clase.Id)).ReturnsAsync(0);
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var request = new UpdateClaseRequest("Pilates", "Clase de pilates", 15, 45, "María López");
        var result = await CrearCommand().ExecuteAsync(clase.Id, request, Guid.NewGuid(), "Admin Test");

        Assert.Equal("Pilates", result.Nombre);
        Assert.Equal(15, result.CapacidadMaxima);
        Assert.Equal(45, result.DuracionMinutos);

        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Modificacion, "Clase", clase.Id,
            It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ClaseNoExiste_LanzaKeyNotFoundException()
    {
        _claseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Clase?)null);

        var request = new UpdateClaseRequest("Yoga", "Desc", 20, 60, "Instructor");

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), request, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_ReducirCapacidadPorDebajoDeInscripciones_LanzaInvalidOperationException()
    {
        var clase = CrearClase(); // capacidad 20
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _claseRepo.Setup(r => r.GetInscripcionesActivasCountAsync(clase.Id)).ReturnsAsync(15);

        var request = new UpdateClaseRequest("Yoga", "Desc", 10, 60, "Instructor"); // 10 < 15

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(clase.Id, request, Guid.NewGuid(), "Admin"));
    }
}
