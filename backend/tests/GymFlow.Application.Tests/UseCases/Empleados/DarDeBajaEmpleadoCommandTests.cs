using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class DarDeBajaEmpleadoCommandTests
{
    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFoundException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var audit = new Mock<IAuditLogger>();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Empleado?)null);
        var sut = new DarDeBajaEmpleadoCommand(emp.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(id, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task NoSePuedeAutoEliminar_LanzaInvalidOperationException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var audit = new Mock<IAuditLogger>();
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "h", Guid.NewGuid());
        var sut = new DarDeBajaEmpleadoCommand(emp.Object, audit.Object);

        // Same id passed as both empleado id and current user id => self-baja attempt
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(empleado.Id, empleado.Id, "Juan"));
    }

    [Fact]
    public async Task HappyPath_DesactivaEmpleado()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var audit = new Mock<IAuditLogger>();
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "h", Guid.NewGuid());
        emp.Setup(r => r.GetByIdAsync(empleado.Id, default)).ReturnsAsync(empleado);
        var sut = new DarDeBajaEmpleadoCommand(emp.Object, audit.Object);

        await sut.ExecuteAsync(empleado.Id, Guid.NewGuid(), "Admin");

        Assert.False(empleado.EstaActivo);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Baja, "Empleado", empleado.Id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
