using System.Security.Cryptography;
using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GymFlow.Infrastructure.Tests.Services;

public class AesGcmMfaSecretProtectorTests
{
    private const string SecretoTotp = "JBSWY3DPEHPK3PXP";

    private static AesGcmMfaSecretProtector CrearProtector()
    {
        // Clave AES-256 (32 bytes) de prueba, en base64, igual que vendría de config.
        var clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Mfa:EncryptionKey"]).Returns(clave);
        return new AesGcmMfaSecretProtector(config.Object);
    }

    [Fact]
    public void ProtectUnprotect_RoundTrip()
    {
        var sut = CrearProtector();

        var blob = sut.Protect(SecretoTotp);
        var recuperado = sut.Unprotect(blob);

        Assert.Equal(SecretoTotp, recuperado);
    }

    [Fact]
    public void Protect_DosVeces_DaResultadosDistintos()
    {
        // Nonce fresco por operación → dos cifrados del mismo input difieren.
        var sut = CrearProtector();

        var blob1 = sut.Protect(SecretoTotp);
        var blob2 = sut.Protect(SecretoTotp);

        Assert.NotEqual(blob1, blob2);
    }

    [Fact]
    public void Unprotect_ConBlobAdulterado_Lanza()
    {
        // Alterar un byte del blob → el tag GCM lo detecta y descifrar falla.
        var sut = CrearProtector();
        var blob = sut.Protect(SecretoTotp);

        var bytes = Convert.FromBase64String(blob);
        bytes[^1] ^= 0xFF; // alteramos el último byte (parte del tag)
        var blobAdulterado = Convert.ToBase64String(bytes);

        Assert.Throws<AuthenticationTagMismatchException>(() => sut.Unprotect(blobAdulterado));
    }
}
