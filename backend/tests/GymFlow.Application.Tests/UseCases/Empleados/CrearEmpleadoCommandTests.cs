using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class CrearEmpleadoCommandTests
{
    private static (Mock<IEmpleadoRepository>, Mock<IRolRepository>, Mock<IPasswordHasher>, Mock<IAuditLogger>) Mocks()
    {
        return (new Mock<IEmpleadoRepository>(), new Mock<IRolRepository>(), new Mock<IPasswordHasher>(), new Mock<IAuditLogger>());
    }

    private static CrearEmpleadoRequest ValidRequest(Guid? rolId = null) =>
        new("Juan", "Pérez", "juan@gymflow.com", "secret123", rolId ?? Guid.NewGuid());

    [Fact]
    public async Task NombreVacio_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest() with { Nombre = "" }, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task PasswordCorta_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest() with { Password = "1234567" }, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task CorreoDuplicado_LanzaInvalidOperationException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        emp.Setup(r => r.ExisteCorreoAsync("juan@gymflow.com", null, default)).ReturnsAsync(true);
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(ValidRequest(), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolInexistente_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var rolId = Guid.NewGuid();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(rolId, default)).ReturnsAsync((Rol?)null);
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest(rolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolEsSocio_LanzaInvalidOperationException()
    {
        var (emp, rol, hasher, audit) = Mocks();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(RolSeed.SocioRolId, default))
            .ReturnsAsync(new Rol(RolSeed.SocioRolId, "Socio", true, DateTime.UtcNow));
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(ValidRequest(RolSeed.SocioRolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task HappyPath_CreaEmpleadoYAuditEs()
    {
        var (emp, rol, hasher, audit) = Mocks();
        var rolId = Guid.NewGuid();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(rolId, default))
            .ReturnsAsync(new Rol(rolId, "Recepcionista", false, DateTime.UtcNow));
        hasher.Setup(h => h.Hash("secret123")).Returns("hashed_secret");
        var sut = new CrearEmpleadoCommand(emp.Object, rol.Object, hasher.Object, audit.Object);

        var dto = await sut.ExecuteAsync(ValidRequest(rolId), Guid.NewGuid(), "Admin");

        Assert.Equal("juan@gymflow.com", dto.Correo);
        Assert.Equal("Recepcionista", dto.RolNombre);
        Assert.True(dto.EstaActivo);
        emp.Verify(r => r.AddAsync(It.Is<Empleado>(e => e.PasswordHash == "hashed_secret"), default), Times.Once);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Creacion, "Empleado", It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
