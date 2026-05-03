using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class CambiarPasswordCommandTests
{
    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFoundException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var audit = new Mock<IAuditLogger>();
        var id = Guid.NewGuid();
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Empleado?)null);
        var sut = new CambiarPasswordCommand(emp.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(id, "newpassword123", Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task PasswordCorta_LanzaArgumentException()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var audit = new Mock<IAuditLogger>();
        var sut = new CambiarPasswordCommand(emp.Object, hasher.Object, audit.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), "1234567", Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task HappyPath_HasheaYAuditEs()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var audit = new Mock<IAuditLogger>();
        var id = Guid.NewGuid();
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "old_hash", Guid.NewGuid());
        emp.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(empleado);
        hasher.Setup(h => h.Hash("newpassword123")).Returns("new_hashed");
        var sut = new CambiarPasswordCommand(emp.Object, hasher.Object, audit.Object);

        await sut.ExecuteAsync(id, "newpassword123", Guid.NewGuid(), "Admin");

        Assert.Equal("new_hashed", empleado.PasswordHash);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Empleado", id, It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
