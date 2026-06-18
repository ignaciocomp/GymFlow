using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth.Mfa;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Auth.Mfa;

public class ResetearMfaEmpleadoCommandTests
{
    private const string Correo = "empleado@gymflow.com";

    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<ICodigoRecuperacionMfaRepository> _codigosRepo = new();
    private readonly Mock<IAuditLogger> _audit = new();

    private ResetearMfaEmpleadoCommand Sut() =>
        new(_empleadoRepo.Object, _codigosRepo.Object, _audit.Object);

    private static Empleado EmpleadoConMfaActivo()
    {
        var empleado = new Empleado("Ana", "Gómez", Correo, "hash-password", RolesSeed.AdminRolId);
        empleado.ActivarMfa("blob-cifrado-base64");
        return empleado;
    }

    [Fact]
    public async Task Reset_LimpiaTodoYAudita()
    {
        var empleado = EmpleadoConMfaActivo();
        // Arrastra fallos previos para verificar que el reset limpia el estado.
        empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);
        var empleadoId = empleado.Id;
        var adminId = Guid.NewGuid();
        const string adminNombre = "Admin Root";

        _empleadoRepo.Setup(r => r.GetByIdAsync(empleadoId, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);

        await Sut().ExecuteAsync(empleadoId, adminId, adminNombre);

        // El segundo factor quedó deshabilitado y su estado limpio.
        Assert.False(empleado.MfaHabilitado);
        Assert.Null(empleado.MfaSecret);
        Assert.Equal(0, empleado.MfaIntentosFallidos);
        Assert.Null(empleado.MfaBloqueadoHasta);

        // Se eliminan los códigos de recuperación del empleado y se persiste todo.
        _codigosRepo.Verify(r => r.EliminarPorEmpleadoAsync(empleadoId), Times.Once);
        _codigosRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Audita el reset a nombre del admin que lo ejecutó.
        _audit.Verify(a => a.LogAsync(
            adminId, adminNombre,
            TipoAccionAuditoria.MfaReseteadoPorAdmin, "Empleado", empleadoId,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task NoSePuedeResetearseASiMismo()
    {
        var adminId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().ExecuteAsync(adminId, adminId, "Admin Root"));

        // No toca el empleado, ni los códigos, ni audita.
        _empleadoRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _codigosRepo.Verify(r => r.EliminarPorEmpleadoAsync(It.IsAny<Guid>()), Times.Never);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _audit.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoAccionAuditoria>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
}
