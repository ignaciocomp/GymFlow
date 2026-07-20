using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class ReactivarEmpleadoCommandTests
{
    private static (Mock<IEmpleadoRepository>, Mock<IRolRepository>, Mock<IAuditLogger>) Mocks()
        => (new Mock<IEmpleadoRepository>(), new Mock<IRolRepository>(), new Mock<IAuditLogger>());

    private static Empleado EmpleadoInactivoSinRol()
    {
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "h", Guid.NewGuid());
        empleado.Desactivar();
        // El rol pudo haber sido eliminado (SetNull en BD). Replicamos ese estado por reflexión.
        typeof(Usuario).GetProperty(nameof(Usuario.RolId))!.SetValue(empleado, null);
        return empleado;
    }

    [Fact]
    public async Task Reactivar_AlRolDueno_PorNoAdmin_Lanza()
    {
        var (emp, rol, audit) = Mocks();
        var empleado = EmpleadoInactivoSinRol();
        emp.Setup(r => r.GetByIdAsync(empleado.Id, default)).ReturnsAsync(empleado);
        rol.Setup(r => r.GetByIdAsync(RolesSeed.DuenoRolId, default))
            .ReturnsAsync(new Rol(RolesSeed.DuenoRolId, "Dueño", true, DateTime.UtcNow));
        var sut = new ReactivarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        var actuanteNoAdmin = Guid.NewGuid();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ExecuteAsync(empleado.Id, RolesSeed.DuenoRolId, Guid.NewGuid(), "Dueño", actuanteNoAdmin));

        Assert.False(empleado.EstaActivo);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Reactivar_AlRolAdmin_PorNoAdmin_Lanza()
    {
        // E2E-21: la reactivación tampoco permite que un no-Admin asigne el rol Admin.
        var (emp, rol, audit) = Mocks();
        var empleado = EmpleadoInactivoSinRol();
        emp.Setup(r => r.GetByIdAsync(empleado.Id, default)).ReturnsAsync(empleado);
        rol.Setup(r => r.GetByIdAsync(RolesSeed.AdminRolId, default))
            .ReturnsAsync(new Rol(RolesSeed.AdminRolId, "Admin", true, DateTime.UtcNow));
        var sut = new ReactivarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        var actuanteNoAdmin = Guid.NewGuid();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ExecuteAsync(empleado.Id, RolesSeed.AdminRolId, Guid.NewGuid(), "Dueño", actuanteNoAdmin));

        Assert.False(empleado.EstaActivo);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Reactivar_AlRolAdmin_PorAdmin_OK()
    {
        var (emp, rol, audit) = Mocks();
        var empleado = EmpleadoInactivoSinRol();
        emp.Setup(r => r.GetByIdAsync(empleado.Id, default)).ReturnsAsync(empleado);
        rol.Setup(r => r.GetByIdAsync(RolesSeed.AdminRolId, default))
            .ReturnsAsync(new Rol(RolesSeed.AdminRolId, "Admin", true, DateTime.UtcNow));
        var sut = new ReactivarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        var dto = await sut.ExecuteAsync(empleado.Id, RolesSeed.AdminRolId, Guid.NewGuid(), "Admin", RolesSeed.AdminRolId);

        Assert.True(empleado.EstaActivo);
        Assert.Equal(RolesSeed.AdminRolId, empleado.RolId);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Reactivar_AlRolDueno_PorAdmin_OK()
    {
        var (emp, rol, audit) = Mocks();
        var empleado = EmpleadoInactivoSinRol();
        emp.Setup(r => r.GetByIdAsync(empleado.Id, default)).ReturnsAsync(empleado);
        rol.Setup(r => r.GetByIdAsync(RolesSeed.DuenoRolId, default))
            .ReturnsAsync(new Rol(RolesSeed.DuenoRolId, "Dueño", true, DateTime.UtcNow));
        var sut = new ReactivarEmpleadoCommand(emp.Object, rol.Object, audit.Object);

        var dto = await sut.ExecuteAsync(empleado.Id, RolesSeed.DuenoRolId, Guid.NewGuid(), "Admin", RolesSeed.AdminRolId);

        Assert.True(empleado.EstaActivo);
        Assert.Equal(RolesSeed.DuenoRolId, empleado.RolId);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
