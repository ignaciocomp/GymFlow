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
    private static (Mock<IEmpleadoRepository>, Mock<IRolRepository>, Mock<IPasswordHasher>, Mock<IAuditLogger>, Mock<IEmailService>) Mocks()
    {
        var email = new Mock<IEmailService>();
        email.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(true));
        return (new Mock<IEmpleadoRepository>(), new Mock<IRolRepository>(), new Mock<IPasswordHasher>(), new Mock<IAuditLogger>(), email);
    }

    private static CrearEmpleadoCommand Sut(
        Mock<IEmpleadoRepository> emp, Mock<IRolRepository> rol, Mock<IPasswordHasher> hasher,
        Mock<IAuditLogger> audit, Mock<IEmailService> email) =>
        new(emp.Object, rol.Object, hasher.Object, audit.Object, email.Object);

    private static CrearEmpleadoRequest ValidRequest(Guid? rolId = null) =>
        new("Juan", "Pérez", "juan@gymflow.com", rolId ?? Guid.NewGuid());

    [Fact]
    public async Task NombreVacio_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit, email) = Mocks();
        var sut = Sut(emp, rol, hasher, audit, email);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest() with { Nombre = "" }, Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task CorreoDuplicado_LanzaInvalidOperationException()
    {
        var (emp, rol, hasher, audit, email) = Mocks();
        emp.Setup(r => r.ExisteCorreoAsync("juan@gymflow.com", null, default)).ReturnsAsync(true);
        var sut = Sut(emp, rol, hasher, audit, email);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(ValidRequest(), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolInexistente_LanzaArgumentException()
    {
        var (emp, rol, hasher, audit, email) = Mocks();
        var rolId = Guid.NewGuid();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(rolId, default)).ReturnsAsync((Rol?)null);
        var sut = Sut(emp, rol, hasher, audit, email);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(ValidRequest(rolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task RolEsSocio_LanzaInvalidOperationException()
    {
        var (emp, rol, hasher, audit, email) = Mocks();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(RolSeed.SocioRolId, default))
            .ReturnsAsync(new Rol(RolSeed.SocioRolId, "Socio", true, DateTime.UtcNow));
        var sut = Sut(emp, rol, hasher, audit, email);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(ValidRequest(RolSeed.SocioRolId), Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public async Task HappyPath_CreaEmpleadoYAuditEs()
    {
        var (emp, rol, hasher, audit, email) = Mocks();
        var rolId = Guid.NewGuid();
        emp.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        rol.Setup(r => r.GetByIdAsync(rolId, default))
            .ReturnsAsync(new Rol(rolId, "Recepcionista", false, DateTime.UtcNow));
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_secret");
        var sut = Sut(emp, rol, hasher, audit, email);

        var dto = await sut.ExecuteAsync(ValidRequest(rolId), Guid.NewGuid(), "Admin");

        Assert.Equal("juan@gymflow.com", dto.Correo);
        Assert.Equal("Recepcionista", dto.RolNombre);
        Assert.True(dto.EstaActivo);
        hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Once);
        emp.Verify(r => r.AddAsync(It.Is<Empleado>(e => e.PasswordHash == "hashed_secret"), default), Times.Once);
        emp.Verify(r => r.SaveChangesAsync(default), Times.Once);
        email.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Creacion, "Empleado", It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
