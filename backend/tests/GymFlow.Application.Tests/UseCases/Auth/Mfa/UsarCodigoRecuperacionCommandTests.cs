using GymFlow.Application.Exceptions;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth.Mfa;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Auth.Mfa;

public class UsarCodigoRecuperacionCommandTests
{
    private const string Correo = "empleado@gymflow.com";
    private const string CodigoIngresado = "ABCDE-12345";
    private const string CodigoIngresadoMalo = "ZZZZZ-99999";

    private readonly Mock<IEmpleadoRepository> _empleadoRepo = new();
    private readonly Mock<ICodigoRecuperacionMfaRepository> _codigosRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAuditLogger> _audit = new();

    private UsarCodigoRecuperacionCommand Sut() =>
        new(_empleadoRepo.Object, _codigosRepo.Object, _hasher.Object, _audit.Object);

    private static Empleado EmpleadoConMfaActivo()
    {
        var empleado = new Empleado("Ana", "Gómez", Correo, "hash-password", RolesSeed.AdminRolId);
        empleado.ActivarMfa("blob-cifrado-base64");
        return empleado;
    }

    private static CodigoRecuperacionMfa CodigoActivo(Guid empleadoId, string hash) =>
        new(empleadoId, hash);

    [Fact]
    public async Task Bloqueado_Lanza()
    {
        var empleado = EmpleadoConMfaActivo();
        for (var i = 0; i < 5; i++) empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);
        Assert.True(empleado.EstaBloqueadoMfa(DateTime.UtcNow));

        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);

        await Assert.ThrowsAsync<MfaBloqueadoException>(
            () => Sut().UsarCodigoRecuperacionAsync(empleado.Id, CodigoIngresado));

        // No se consultan ni consumen códigos si está bloqueado.
        _codigosRepo.Verify(r => r.GetActivosPorEmpleadoAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CodigoRecuperacionValido_ConsumeYEntra()
    {
        var empleado = EmpleadoConMfaActivo();
        // Arrastra fallos previos que deben limpiarse al entrar con un código válido.
        empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);
        empleado.RegistrarIntentoFallidoMfa(DateTime.UtcNow);

        var codigoOtro = CodigoActivo(empleado.Id, "hash-otro");
        var codigoBueno = CodigoActivo(empleado.Id, "hash-bueno");
        var activos = new List<CodigoRecuperacionMfa> { codigoOtro, codigoBueno };

        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _codigosRepo.Setup(r => r.GetActivosPorEmpleadoAsync(empleado.Id)).ReturnsAsync(activos);
        _hasher.Setup(h => h.Verify(CodigoIngresado, "hash-otro")).Returns(false);
        _hasher.Setup(h => h.Verify(CodigoIngresado, "hash-bueno")).Returns(true);

        var resultado = await Sut().UsarCodigoRecuperacionAsync(empleado.Id, CodigoIngresado);

        // Devuelve el empleado para emitir el JWT.
        Assert.Same(empleado, resultado);

        // Consumió el código correcto (de un solo uso) y reseteó el contador.
        Assert.True(codigoBueno.Usado);
        Assert.NotNull(codigoBueno.FechaUso);
        Assert.False(codigoOtro.Usado);
        Assert.Equal(0, empleado.MfaIntentosFallidos);
        Assert.Null(empleado.MfaBloqueadoHasta);

        _codigosRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _audit.Verify(a => a.LogAsync(
            empleado.Id, It.IsAny<string>(),
            TipoAccionAuditoria.MfaCodigoRecuperacionUsado, "Empleado", empleado.Id,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CodigoRecuperacionInvalido_SumaIntento()
    {
        var empleado = EmpleadoConMfaActivo();
        var activos = new List<CodigoRecuperacionMfa> { CodigoActivo(empleado.Id, "hash-otro") };

        _empleadoRepo.Setup(r => r.GetByIdAsync(empleado.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        _codigosRepo.Setup(r => r.GetActivosPorEmpleadoAsync(empleado.Id)).ReturnsAsync(activos);
        _hasher.Setup(h => h.Verify(CodigoIngresadoMalo, It.IsAny<string>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Sut().UsarCodigoRecuperacionAsync(empleado.Id, CodigoIngresadoMalo));

        // Cuenta para el mismo lockout (suma intento fallido) y persiste el empleado.
        Assert.Equal(1, empleado.MfaIntentosFallidos);
        _empleadoRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // No consume ningún código.
        _codigosRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _audit.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), TipoAccionAuditoria.MfaCodigoRecuperacionUsado,
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
}
