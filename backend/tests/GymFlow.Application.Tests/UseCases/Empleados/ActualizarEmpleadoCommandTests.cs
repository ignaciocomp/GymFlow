using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class ActualizarEmpleadoCommandTests
{
    private static (Mock<IEmpleadoRepository>, Mock<IRolRepository>, Mock<IAuditLogger>) Mocks()
        => (new Mock<IEmpleadoRepository>(), new Mock<IRolRepository>(), new Mock<IAuditLogger>());

    private static Empleado ExistingEmpleado(Guid? rolId = null) =>
        new("Juan", "Pérez", "juan@gymflow.com", "old_hash", rolId ?? Guid.NewGuid());

    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFoundException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Empleado?)null);
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan", "Pérez", "juan@gymflow.com", Guid.NewGuid()), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolEsSocio_LanzaInvalidOperationException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(ExistingEmpleado());
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(RolesSeed.SocioRolId, default))
            .ReturnsAsync(new Rol(RolesSeed.SocioRolId, "Socio", true, DateTime.UtcNow));
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan", "Pérez", "juan@gymflow.com", RolesSeed.SocioRolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task CorreoDuplicado_LanzaInvalidOperationException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(ExistingEmpleado());
        emp.Setup(r => r.ExisteCorreoAsync("otro@gymflow.com", id, default)).ReturnsAsync(true);
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan", "Pérez", "otro@gymflow.com", Guid.NewGuid()), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolInexistente_LanzaArgumentException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        var rolId = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(ExistingEmpleado());
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(rolId, default)).ReturnsAsync((Rol?)null);
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan", "Pérez", "juan@gymflow.com", rolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task HappyPath_ActualizaYAuditEs()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        var nuevoRolId = Guid.NewGuid();
        var empleado = ExistingEmpleado();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(empleado);
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(nuevoRolId, default))
            .ReturnsAsync(new Rol(nuevoRolId, "Encargado", false, DateTime.UtcNow));
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await sut.ExecuteAsync(id, new ActualizarEmpleadoRequest("Juan Carlos", "Pérez", "jc@gymflow.com", nuevoRolId), Guid.NewGuid(), "Admin");

        Assert.Equal("Juan Carlos", empleado.Nombre);
        Assert.Equal("jc@gymflow.com", empleado.Correo);
        Assert.Equal(nuevoRolId, empleado.RolId);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Empleado", id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
