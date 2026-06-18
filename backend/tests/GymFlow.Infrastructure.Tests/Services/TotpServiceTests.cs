using GymFlow.Infrastructure.Services;
using OtpNet;

namespace GymFlow.Infrastructure.Tests.Services;

public class TotpServiceTests
{
    private static TotpService CrearServicio() => new();

    [Fact]
    public void GenerarSecreto_DevuelveBase32Valido()
    {
        var sut = CrearServicio();

        var secreto = sut.GenerarSecreto();

        Assert.False(string.IsNullOrWhiteSpace(secreto));
        // Base32 decodificable y de 160 bits (20 bytes).
        var bytes = Base32Encoding.ToBytes(secreto);
        Assert.Equal(20, bytes.Length);
    }

    [Fact]
    public void ValidarCodigo_ConCodigoGeneradoAhora_True()
    {
        var sut = CrearServicio();
        var secreto = sut.GenerarSecreto();

        // Generamos el código actual con el mismo secreto, vía Otp.NET, igual
        // que haría una app autenticadora.
        var totp = new Totp(Base32Encoding.ToBytes(secreto));
        var codigoActual = totp.ComputeTotp();

        Assert.True(sut.ValidarCodigo(secreto, codigoActual));
    }

    [Fact]
    public void ValidarCodigo_ConCodigoIncorrecto_False()
    {
        var sut = CrearServicio();
        var secreto = sut.GenerarSecreto();

        Assert.False(sut.ValidarCodigo(secreto, "000000"));
    }

    [Fact]
    public void GenerarUri_DevuelveOtpauthConIssuerYCuenta()
    {
        var sut = CrearServicio();
        var secreto = sut.GenerarSecreto();

        var uri = sut.GenerarUriOtpauth(secreto, "empleado@gymflow.com");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains("GymFlow", uri);
        Assert.Contains("empleado%40gymflow.com", uri);
    }

    [Fact]
    public void GenerarCodigosRecuperacion_Devuelve10Distintos()
    {
        var sut = CrearServicio();

        var codigos = sut.GenerarCodigosRecuperacion();

        Assert.Equal(10, codigos.Count);
        Assert.Equal(10, codigos.Distinct().Count());
        // ~50 bits de entropía: 10 chars base32 (5 bits c/u) → 50 bits.
        Assert.All(codigos, c => Assert.Equal(10, c.Length));
    }
}
