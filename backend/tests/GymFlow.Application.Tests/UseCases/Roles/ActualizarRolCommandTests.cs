using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Roles;

public class ActualizarRolCommandTests
{
    [Fact]
    public async Task RolNoExiste_LanzaKeyNotFoundException()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Rol?)null);

        var sut = new ActualizarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), new ActualizarRolRequest("X", new List<Guid>()), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Administrador", esSistema: true);
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);

        var sut = new ActualizarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(rol.Id, new ActualizarRolRequest("X", new List<Guid>()), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task NombreDuplicado_LanzaInvalidOperationException()
    {
        var rol = new Rol("Original");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.ExisteConNombreAsync("Otro", rol.Id, default)).ReturnsAsync(true);

        var sut = new ActualizarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(rol.Id, new ActualizarRolRequest("Otro", new List<Guid>()), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task HappyPath_ActualizaInvalidaCacheYAuditEs()
    {
        var rol = new Rol("Original");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.ExisteConNombreAsync(It.IsAny<string>(), rol.Id, default)).ReturnsAsync(false);
        var cache = new Mock<IPermisoCache>();
        var audit = new Mock<IAuditLogger>();

        var sut = new ActualizarRolCommand(repo.Object, cache.Object, audit.Object);

        await sut.ExecuteAsync(rol.Id, new ActualizarRolRequest("Nuevo", new List<Guid>()), Guid.NewGuid(), "T");

        Assert.Equal("Nuevo", rol.Nombre);
        cache.Verify(c => c.Invalidar(rol.Id), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<GymFlow.Domain.Enums.TipoAccionAuditoria>(), "Rol", It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
