using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Roles;

public class EliminarRolCommandTests
{
    [Fact]
    public async Task RolNoExiste_LanzaKeyNotFoundException()
    {
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Rol?)null);

        var sut = new EliminarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Admin", esSistema: true);
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);

        var sut = new EliminarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(rol.Id, Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task RolConUsuariosActivos_LanzaInvalidOperationException()
    {
        var rol = new Rol("Recepcionista");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.TieneUsuariosActivosAsignadosAsync(rol.Id, default)).ReturnsAsync(true);

        var sut = new EliminarRolCommand(repo.Object, Mock.Of<IPermisoCache>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(rol.Id, Guid.NewGuid(), "T"));
    }

    [Fact]
    public async Task HappyPath_EliminaInvalidaCacheYAuditEs()
    {
        var rol = new Rol("Recepcionista");
        var repo = new Mock<IRolRepository>();
        repo.Setup(r => r.GetByIdAsync(rol.Id, default)).ReturnsAsync(rol);
        repo.Setup(r => r.TieneUsuariosActivosAsignadosAsync(rol.Id, default)).ReturnsAsync(false);
        var cache = new Mock<IPermisoCache>();
        var audit = new Mock<IAuditLogger>();

        var sut = new EliminarRolCommand(repo.Object, cache.Object, audit.Object);
        await sut.ExecuteAsync(rol.Id, Guid.NewGuid(), "T");

        repo.Verify(r => r.Remove(rol), Times.Once);
        cache.Verify(c => c.Invalidar(rol.Id), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(), GymFlow.Domain.Enums.TipoAccionAuditoria.Baja, "Rol", rol.Id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
