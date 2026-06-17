using System.Security.Cryptography;
using System.Text;
using GymFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Protege el secreto TOTP en reposo con AES-256-GCM (nativo de .NET 8).
/// Por cada cifrado usa un nonce aleatorio de 96 bits (nunca reusado bajo la
/// misma clave) y un tag de autenticación de 128 bits. El blob persistido es
/// base64(nonce ‖ ciphertext ‖ tag); el tag detecta cualquier adulteración al
/// descifrar. La clave AES-256 viene de configuración (Mfa:EncryptionKey, base64
/// de 32 bytes) y se valida en el constructor.
/// </summary>
public class AesGcmMfaSecretProtector : IMfaSecretProtector
{
    private const int TamanioNonceBytes = 12; // 96 bits
    private const int TamanioTagBytes = 16;    // 128 bits
    private const int TamanioClaveBytes = 32;  // AES-256

    private readonly byte[] _clave;

    public AesGcmMfaSecretProtector(IConfiguration configuration)
    {
        var claveBase64 = configuration["Mfa:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(claveBase64))
        {
            throw new InvalidOperationException(
                "Falta la configuración 'Mfa:EncryptionKey' para el cifrado del secreto MFA.");
        }

        byte[] clave;
        try
        {
            clave = Convert.FromBase64String(claveBase64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "'Mfa:EncryptionKey' debe ser una clave AES-256 codificada en base64.");
        }

        if (clave.Length != TamanioClaveBytes)
        {
            throw new InvalidOperationException(
                $"'Mfa:EncryptionKey' debe ser de {TamanioClaveBytes} bytes (AES-256); " +
                $"se recibieron {clave.Length}.");
        }

        _clave = clave;
    }

    public string Protect(string textoPlano)
    {
        ArgumentNullException.ThrowIfNull(textoPlano);

        var nonce = RandomNumberGenerator.GetBytes(TamanioNonceBytes);
        var plano = Encoding.UTF8.GetBytes(textoPlano);
        var cifrado = new byte[plano.Length];
        var tag = new byte[TamanioTagBytes];

        using var aes = new AesGcm(_clave, TamanioTagBytes);
        aes.Encrypt(nonce, plano, cifrado, tag);

        // base64(nonce ‖ ciphertext ‖ tag)
        var blob = new byte[nonce.Length + cifrado.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, blob, 0, nonce.Length);
        Buffer.BlockCopy(cifrado, 0, blob, nonce.Length, cifrado.Length);
        Buffer.BlockCopy(tag, 0, blob, nonce.Length + cifrado.Length, tag.Length);

        return Convert.ToBase64String(blob);
    }

    public string Unprotect(string blob)
    {
        ArgumentNullException.ThrowIfNull(blob);

        var datos = Convert.FromBase64String(blob);
        if (datos.Length < TamanioNonceBytes + TamanioTagBytes)
        {
            throw new ArgumentException("El blob protegido es demasiado corto para ser válido.", nameof(blob));
        }

        var nonce = new byte[TamanioNonceBytes];
        var tag = new byte[TamanioTagBytes];
        var cifrado = new byte[datos.Length - TamanioNonceBytes - TamanioTagBytes];

        Buffer.BlockCopy(datos, 0, nonce, 0, TamanioNonceBytes);
        Buffer.BlockCopy(datos, TamanioNonceBytes, cifrado, 0, cifrado.Length);
        Buffer.BlockCopy(datos, TamanioNonceBytes + cifrado.Length, tag, 0, TamanioTagBytes);

        var plano = new byte[cifrado.Length];

        using var aes = new AesGcm(_clave, TamanioTagBytes);
        // Lanza AuthenticationTagMismatchException si el blob fue adulterado.
        aes.Decrypt(nonce, cifrado, tag, plano);

        return Encoding.UTF8.GetString(plano);
    }
}
