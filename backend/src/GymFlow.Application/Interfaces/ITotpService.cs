namespace GymFlow.Application.Interfaces;

/// <summary>
/// Operaciones TOTP (RFC 6238) para el segundo factor de empleados y la
/// generación de códigos de recuperación. La implementación (Infrastructure)
/// usa Otp.NET con los defaults de los authenticators (6 dígitos, período 30s,
/// HMAC-SHA1) y una ventana de validación de ±1 step.
/// </summary>
public interface ITotpService
{
    /// <summary>Genera un secreto TOTP nuevo (160 bits) codificado en base32.</summary>
    string GenerarSecreto();

    /// <summary>
    /// Valida un código de 6 dígitos contra el secreto base32, tolerando un
    /// desfasaje de reloj de ±1 step (±30s).
    /// </summary>
    bool ValidarCodigo(string secreto, string codigo);

    /// <summary>
    /// Arma el URI <c>otpauth://totp/...</c> para el QR / alta manual, con
    /// issuer "GymFlow" y la cuenta (correo) del empleado.
    /// </summary>
    string GenerarUriOtpauth(string secreto, string cuenta);

    /// <summary>
    /// Genera 10 códigos de recuperación de un solo uso en claro (~50 bits de
    /// entropía cada uno). El hash de persistencia lo hace el command con
    /// <see cref="IPasswordHasher"/>; este servicio NO hashea.
    /// </summary>
    IReadOnlyList<string> GenerarCodigosRecuperacion();
}
