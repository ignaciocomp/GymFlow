using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Roles;

public class CrearRolCommandTests
{
    [Fact]
    public async Task NombreVacio_LanzaArgumentException()
    {
        var sut = new CrearRolCommand(Mock.Of<IRolRepository>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(new CrearRolRequest("", new List<Guid>()), Guid.NewGuid(), "Test"));
    }

    [Fact]
    public async Task NombreDuplicado_LanzaInvalidOperationException()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.ExisteConNombreAsync("Recepcionista", null, default)).ReturnsAsync(true);

        var sut = new CrearRolCommand(repo.Object, Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(new CrearRolRequest("Recepcionista", new List<Guid>()), Guid.NewGuid(), "Test"));
    }

    [Fact]
    public async Task HappyPath_CreaRolYAuditEs()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.ExisteConNombreAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        var audit = new Mock<IAuditLogger>();

        var sut = new CrearRolCommand(repo.Object, audit.Object);
        var permiso1 = Guid.NewGuid();

        var dto = await sut.ExecuteAsync(
            new CrearRolRequest("Recepcionista", new[] { permiso1 }),
            Guid.NewGuid(), "Admin Test");

        Assert.Equal("Recepcionista", dto.Nombre);
        Assert.False(dto.EsSistema);
        repo.Verify(r => r.AddAsync(It.IsAny<Rol>(), default), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Creacion, "Rol", It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
