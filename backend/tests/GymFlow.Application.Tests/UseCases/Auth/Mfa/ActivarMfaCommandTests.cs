using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth.Mfa;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Auth.Mfa;

public class ActivarMfaCommandTests
{
    private const string Correo = "empleado@gymflow.com";
    private const string SecretoClaro = "JBSWY3DPEHPK3PXP";
    private const string SecretoCifrado = "blob-cifrado-base64";
    private const string CodigoValido = "123456";
    private const string CodigoInvalido = "000000";

    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<ICodigoRecuperacionMfaRepository> _codigosRepo = new();
    private readonly Mock<ITotpService> _totp = new();
    private readonly Mock<IMfaSecretProtector> _protector = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAuditLogger> _audit = new();

    private ActivarMfaCommand Sut() =>
        new(_empleadoRepo.Object, _codigosRepo.Object, _totp.Object, _protector.Object, _hasher.Object, _audit.Object);

    private static Empleado EmpleadoConSecreto()
    {
        var empleado = new Empleado("Ana", "Gómez", Correo, "hash-password", RolesSeed.AdminRolId);
        // El setup ya guardó el secreto cifrado, sin habilitar el MFA.
        empleado.PrepararSecretoMfa(SecretoCifrado);
        return empleado;
    }

    [Fact]
    public async Task CodigoValido_ActivaYGeneraCodigos()
    {
        var empleado = EmpleadoConSecreto();
        var empleadoId = empleado.Id;
        _empleadoRepo.Setup(r => r.GetByIdAsync(empleadoId, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _protector.Setup(p => p.Unprotect(SecretoCifrado)).Returns(SecretoClaro);
        _totp.Setup(t => t.ValidarCodigo(SecretoClaro, CodigoValido)).Returns(true);

        var codigosClaro = new List<string> { "AAA-111", "BBB-222", "CCC-333", "DDD-444", "EEE-555",
                                              "FFF-666", "GGG-777", "HHH-888", "III-999", "JJJ-000" };
        _totp.Setup(t => t.GenerarCodigosRecuperacion()).Returns(codigosClaro);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(c => $"hash:{c}");

        List<CodigoRecuperacionMfa>? persistidos = null;
        _codigosRepo.Setup(r => r.AgregarRangoAsync(It.IsAny<IEnumerable<CodigoRecuperacionMfa>>()))
            .Callback<IEnumerable<CodigoRecuperacionMfa>>(c => persistidos = c.ToList())
            .Returns(Task.CompletedTask);

        var resultado = await Sut().ActivarMfaAsync(empleadoId, CodigoValido);

        // Devuelve los 10 códigos en claro (una sola vez).
        Assert.Equal(codigosClaro, resultado.CodigosRecuperacion);

        // El MFA queda habilitado conservando el secreto.
        Assert.True(empleado.MfaHabilitado);
        Assert.Equal(SecretoCifrado, empleado.MfaSecret);

        // Se persistieron los 10 códigos HASHEADOS (nunca en claro), uno por empleado.
        Assert.NotNull(persistidos);
        Assert.Equal(10, persistidos!.Count);
        Assert.All(persistidos, c => Assert.Equal(empleadoId, c.EmpleadoId));
        Assert.All(persistidos, c => Assert.StartsWith("hash:", c.CodigoHash));
        Assert.DoesNotContain(persistidos, c => codigosClaro.Contains(c.CodigoHash));

        _hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Exactly(10));
        _codigosRepo.Verify(r => r.AgregarRangoAsync(It.IsAny<IEnumerable<CodigoRecuperacionMfa>>()), Times.Once);
        _codigosRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Audita la activación.
        _audit.Verify(a => a.LogAsync(
            empleadoId, It.IsAny<string>(),
            TipoAccionAuditoria.MfaActivado, "Empleado", empleadoId,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CodigoInvalido_NoActiva_Lanza()
    {
        var empleado = EmpleadoConSecreto();
        var empleadoId = empleado.Id;
        _empleadoRepo.Setup(r => r.GetByIdAsync(empleadoId, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _protector.Setup(p => p.Unprotect(SecretoCifrado)).Returns(SecretoClaro);
        _totp.Setup(t => t.ValidarCodigo(SecretoClaro, CodigoInvalido)).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Sut().ActivarMfaAsync(empleadoId, CodigoInvalido));

        // No se habilita el MFA ni se generan/persisten códigos.
        Assert.False(empleado.MfaHabilitado);
        _totp.Verify(t => t.GenerarCodigosRecuperacion(), Times.Never);
        _codigosRepo.Verify(r => r.AgregarRangoAsync(It.IsAny<IEnumerable<CodigoRecuperacionMfa>>()), Times.Never);
        _codigosRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _audit.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoAccionAuditoria>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
}
