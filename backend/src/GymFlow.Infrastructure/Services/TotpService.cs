using GymFlow.Application.Interfaces;
using OtpNet;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Implementación TOTP (RFC 6238) sobre Otp.NET con los defaults de los
/// authenticators (6 dígitos, período 30s, HMAC-SHA1) y ventana de validación
/// de ±1 step. También genera los códigos de recuperación en claro (~50 bits
/// cada uno); el hash de persistencia lo hace el command con IPasswordHasher.
/// </summary>
public class TotpService : ITotpService
{
    private const string Issuer = "GymFlow";
    private const int TamanioSecretoBytes = 20; // 160 bits
    private const int CantidadCodigosRecuperacion = 10;
    private const int LongitudCodigoRecuperacion = 10; // 10 chars base32 ≈ 50 bits

    // Ventana de verificación de ±1 step (±30s) por desfasaje de reloj.
    private static readonly VerificationWindow Ventana = new(previous: 1, future: 1);

    public string GenerarSecreto()
    {
        var clave = KeyGeneration.GenerateRandomKey(TamanioSecretoBytes);
        return Base32Encoding.ToString(clave);
    }

    public bool ValidarCodigo(string secreto, string codigo)
    {
        if (string.IsNullOrWhiteSpace(secreto) || string.IsNullOrWhiteSpace(codigo))
            return false;

        try
        {
            var totp = new Totp(Base32Encoding.ToBytes(secreto));
            return totp.VerifyTotp(codigo, out _, Ventana);
        }
        catch
        {
            // Secreto base32 inválido u otro formato inesperado → no valida.
            return false;
        }
    }

    public string GenerarUriOtpauth(string secreto, string cuenta)
    {
        var uri = new OtpUri(OtpType.Totp, secreto, cuenta, Issuer);
        return uri.ToString();
    }

    public IReadOnlyList<string> GenerarCodigosRecuperacion()
    {
        var codigos = new List<string>(CantidadCodigosRecuperacion);
        while (codigos.Count < CantidadCodigosRecuperacion)
        {
            // Cada código: 10 chars base32 (5 bits c/u) ≈ 50 bits de entropía.
            // Generamos suficientes bytes aleatorios y recortamos el base32.
            var bytes = KeyGeneration.GenerateRandomKey(TamanioSecretoBytes);
            var codigo = Base32Encoding.ToString(bytes)[..LongitudCodigoRecuperacion];
            if (!codigos.Contains(codigo))
                codigos.Add(codigo);
        }

        return codigos;
    }
}
