using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth.Mfa;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Auth.Mfa;

public class IniciarMfaSetupCommandTests
{
    private const string Correo = "empleado@gymflow.com";
    private const string SecretoClaro = "JBSWY3DPEHPK3PXP";
    private const string SecretoCifrado = "blob-cifrado-base64";
    private const string Uri = "otpauth://totp/GymFlow:empleado@gymflow.com?secret=JBSWY3DPEHPK3PXP&issuer=GymFlow";

    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<ITotpService> _totp = new();
    private readonly Mock<IMfaSecretProtector> _protector = new();

    private IniciarMfaSetupCommand Sut() => new(_empleadoRepo.Object, _totp.Object, _protector.Object);

    private static Empleado EmpleadoFake() =>
        new("Ana", "Gómez", Correo, "hash-password", RolesSeed.AdminRolId);

    [Fact]
    public async Task EmpleadoSinMfa_GeneraCifraYPersisteSecreto_SinHabilitar()
    {
        var empleado = EmpleadoFake();
        var empleadoId = empleado.Id;
        _empleadoRepo.Setup(r => r.GetByIdAsync(empleadoId, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _totp.Setup(t => t.GenerarSecreto()).Returns(SecretoClaro);
        _protector.Setup(p => p.Protect(SecretoClaro)).Returns(SecretoCifrado);
        _totp.Setup(t => t.GenerarUriOtpauth(SecretoClaro, Correo)).Returns(Uri);

        var resultado = await Sut().IniciarMfaSetupAsync(empleadoId);

        // Devuelve el secreto en claro (para el QR / alta manual) y el URI.
        Assert.Equal(SecretoClaro, resultado.SecretoBase32);
        Assert.Equal(Uri, resultado.UriOtpauth);

        // Persiste el secreto CIFRADO en el empleado, sin habilitar el MFA.
        Assert.Equal(SecretoCifrado, empleado.MfaSecret);
        Assert.False(empleado.MfaHabilitado);

        _protector.Verify(p => p.Protect(SecretoClaro), Times.Once);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmpleadoInexistente_LanzaKeyNotFound()
    {
        var empleadoId = Guid.NewGuid();
        _empleadoRepo.Setup(r => r.GetByIdAsync(empleadoId, It.IsAny<CancellationToken>())).ReturnsAsync((Empleado?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Sut().IniciarMfaSetupAsync(empleadoId));

        _totp.Verify(t => t.GenerarSecreto(), Times.Never);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
