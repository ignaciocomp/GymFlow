using GymFlow.Application.Exceptions;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth.Mfa;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Auth.Mfa;

public class VerificarMfaCommandTests
{
    private const string Correo = "empleado@gymflow.com";
    private const string SecretoClaro = "JBSWY3DPEHPK3PXP";
    private const string SecretoCifrado = "blob-cifrado-base64";
    private const string CodigoValido = "123456";
    private const string CodigoInvalido = "000000";

    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<ITotpService> _totp = new();
    private readonly Mock<IMfaSecretProtector> _protector = new();
    private readonly Mock<IAuditLogger> _audit = new();

    private VerificarMfaCommand Sut() =>
        new(_empleadoRepo.Object, _totp.Object, _protector.Object, _audit.Object);

    /// <summary>Empleado con MFA ya activado (secreto cifrado persistido).</summary>
    private static Empleado EmpleadoConMfaActivo()
    {
        var empleado = new Empleado("Ana", "Gómez", Correo, "hash-password", RolesSeed.AdminRolId);
        empleado.ActivarMfa(SecretoCifrado);
        return empleado;
    }

    [Fact]
    public async Task Bloqueado_Lanza()
    {
        var empleado = EmpleadoConMfaActivo();
        // Forzar el bloqueo: 5 intentos fallidos previos.
        for (var i = 0; i < 5; i++) empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);
        Assert.True(empleado.EstaBloqueadoMfa(DateTime.UtcNow));

        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);

        await Assert.ThrowsAsync<MfaBloqueadoException>(() => Sut().VerificarMfaAsync(empleado.Id, CodigoValido));

        // No se intenta validar el código si está bloqueado.
        _protector.Verify(p => p.Unprotect(It.IsAny<string>()), Times.Never);
        _totp.Verify(t => t.ValidarCodigo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CodigoValido_DevuelveEmpleadoYResetea()
    {
        var empleado = EmpleadoConMfaActivo();
        // Arrastra un par de intentos fallidos previos que la verificación debe limpiar.
        empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);
        empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);

        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _protector.Setup(p => p.Unprotect(SecretoCifrado)).Returns(SecretoClaro);
        _totp.Setup(t => t.ValidarCodigo(SecretoClaro, CodigoValido)).Returns(true);

        var resultado = await Sut().VerificarMfaAsync(empleado.Id, CodigoValido);

        // Devuelve el empleado para que la API emita el JWT.
        Assert.Same(empleado, resultado);

        // Resetea el contador de intentos fallidos.
        Assert.Equal(0, empleado.MfaIntentosFallidos);
        Assert.Null(empleado.MfaBloqueadoHasta);

        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Audita la verificación exitosa.
        _audit.Verify(a => a.LogAsync(
            empleado.Id, It.IsAny<string>(),
            TipoAccionAuditoria.MfaVerificado, "Empleado", empleado.Id,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CodigoInvalido_SumaIntento_Lanza()
    {
        var empleado = EmpleadoConMfaActivo();
        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _protector.Setup(p => p.Unprotect(SecretoCifrado)).Returns(SecretoClaro);
        _totp.Setup(t => t.ValidarCodigo(SecretoClaro, CodigoInvalido)).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Sut().VerificarMfaAsync(empleado.Id, CodigoInvalido));

        // Sumó un intento fallido y lo persistió.
        Assert.Equal(1, empleado.MfaIntentosFallidos);
        Assert.False(empleado.EstaBloqueadoMfa(DateTime.UtcNow));
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // No audita verificación exitosa.
        _audit.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), TipoAccionAuditoria.MfaVerificado,
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task QuintoFallo_Bloquea()
    {
        var empleado = EmpleadoConMfaActivo();
        // Cuatro fallos previos: el de este command será el quinto y bloquea.
        for (var i = 0; i < 4; i++) empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);
        Assert.False(empleado.EstaBloqueadoMfa(DateTime.UtcNow));

        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _protector.Setup(p => p.Unprotect(SecretoCifrado)).Returns(SecretoClaro);
        _totp.Setup(t => t.ValidarCodigo(SecretoClaro, CodigoInvalido)).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Sut().VerificarMfaAsync(empleado.Id, CodigoInvalido));

        // Quedó bloqueado y se auditó el bloqueo.
        Assert.Equal(5, empleado.MfaIntentosFallidos);
        Assert.True(empleado.EstaBloqueadoMfa(DateTime.UtcNow));
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.LogAsync(
            empleado.Id, It.IsAny<string>(),
            TipoAccionAuditoria.MfaBloqueado, "Empleado", empleado.Id,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
