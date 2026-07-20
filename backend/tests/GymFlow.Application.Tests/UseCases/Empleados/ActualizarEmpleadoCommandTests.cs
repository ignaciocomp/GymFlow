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

    private static ActualizarEmpleadoRequest Req(string nombre, string apellido, string correo, Guid rolId, Guid[]? unidadIds = null) =>
        new(nombre, apellido, correo, rolId, unidadIds ?? []);

    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFoundException()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Empleado?)null);
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(id, Req("Juan", "Pérez", "juan@gymflow.com", Guid.NewGuid()), Guid.NewGuid(), "Admin", RolesSeed.AdminRolId));
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
            sut.ExecuteAsync(id, Req("Juan", "Pérez", "juan@gymflow.com", RolesSeed.SocioRolId), Guid.NewGuid(), "Admin", RolesSeed.AdminRolId));
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
            sut.ExecuteAsync(id, Req("Juan", "Pérez", "otro@gymflow.com", Guid.NewGuid()), Guid.NewGuid(), "Admin", RolesSeed.AdminRolId));
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
            sut.ExecuteAsync(id, Req("Juan", "Pérez", "juan@gymflow.com", rolId), Guid.NewGuid(), "Admin", RolesSeed.AdminRolId));
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

        await sut.ExecuteAsync(id, Req("Juan Carlos", "Pérez", "jc@gymflow.com", nuevoRolId), Guid.NewGuid(), "Admin", RolesSeed.AdminRolId);

        Assert.Equal("Juan Carlos", empleado.Nombre);
        Assert.Equal("jc@gymflow.com", empleado.Correo);
        Assert.Equal(nuevoRolId, empleado.RolId);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Empleado", id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Actualizar_AlRolAdmin_PorNoAdmin_Lanza()
    {
        // E2E-21: un actuante no-Admin tampoco puede escalar a otro empleado al rol Admin.
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        var empleado = ExistingEmpleado();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(empleado);
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(RolesSeed.AdminRolId, default))
            .ReturnsAsync(new Rol(RolesSeed.AdminRolId, "Admin", true, DateTime.UtcNow));
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        var actuanteNoAdmin = Guid.NewGuid();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ExecuteAsync(id, Req("Juan", "Pérez", "juan@gymflow.com", RolesSeed.AdminRolId), Guid.NewGuid(), "Dueño", actuanteNoAdmin));

        emp.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Actualizar_AlRolDueno_PorNoAdmin_Lanza()
    {
        var (emp, rol, audit) = Mocks();
        var id = Guid.NewGuid();
        var empleado = ExistingEmpleado();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(empleado);
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), id, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(RolesSeed.DuenoRolId, default))
            .ReturnsAsync(new Rol(RolesSeed.DuenoRolId, "Dueño", true, DateTime.UtcNow));
        var sut = new ActualizarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        var actuanteNoAdmin = Guid.NewGuid();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ExecuteAsync(id, Req("Juan", "Pérez", "juan@gymflow.com", RolesSeed.DuenoRolId, [Guid.NewGuid()]), Guid.NewGuid(), "Dueño", actuanteNoAdmin));

        emp.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
