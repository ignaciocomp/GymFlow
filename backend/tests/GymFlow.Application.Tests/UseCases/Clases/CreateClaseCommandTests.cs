using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Clases;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Clases;

public class CreateClaseCommandTests
{
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IUnidadRepository> _unidadRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private CreateClaseCommand CrearCommand() =>
        new(_claseRepo.Object, _unidadRepo.Object, _auditLogger.Object);

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_CreaClaseYRegistraAuditoria()
    {
        var unidadId = Guid.NewGuid();
        _unidadRepo.Setup(r => r.GetByIdAsync(unidadId))
            .ReturnsAsync(new Unidad("Espacio Mora", "Av. 8 de Octubre 2845"));
        _claseRepo.Setup(r => r.AddAsync(It.IsAny<Clase>())).Returns(Task.CompletedTask);
        _claseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var request = new CreateClaseRequest("Yoga", "Clase de yoga", 20, 60, "Laura García", unidadId);
        var result = await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin Test");

        Assert.Equal("Yoga", result.Nombre);
        Assert.Equal(20, result.CapacidadMaxima);
        Assert.Equal("Espacio Mora", result.UnidadNombre);
        Assert.True(result.EstaActivo);

        _claseRepo.Verify(r => r.AddAsync(It.IsAny<Clase>()), Times.Once);
        _claseRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Creacion, "Clase", It.IsAny<Guid>(),
            It.Is<string>(s => s.Contains("Yoga")), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConUnidadInexistente_LanzaArgumentException()
    {
        _unidadRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Unidad?)null);

        var request = new CreateClaseRequest("Yoga", "Desc", 20, 60, "Instructor", Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin"));

        _claseRepo.Verify(r => r.AddAsync(It.IsAny<Clase>()), Times.Never);
    }
}
