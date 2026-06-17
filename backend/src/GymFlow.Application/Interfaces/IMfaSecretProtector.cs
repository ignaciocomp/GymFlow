namespace GymFlow.Application.Interfaces;

/// <summary>
/// Cifra y descifra el secreto TOTP de un empleado para guardarlo en reposo.
/// Implementación en Infrastructure con AES-256-GCM (nonce único por operación).
/// </summary>
public interface IMfaSecretProtector
{
    /// <summary>Cifra un texto plano y devuelve el blob protegido (base64).</summary>
    string Protect(string textoPlano);

    /// <summary>Descifra un blob protegido y devuelve el texto plano original.</summary>
    string Unprotect(string blob);
}
