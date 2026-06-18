namespace GymFlow.Application.Interfaces;

/// <summary>
/// Emite y valida el token intermedio de MFA (<c>mfaToken</c>) del login en dos pasos.
/// Se firma con una clave DEDICADA (<c>Mfa:TokenSigningKey</c>), distinta de <c>Jwt:Key</c>,
/// para que el pipeline JWT global lo rechace en endpoints normales. Lleva solo los claims
/// <c>sub</c> (userId), <c>purpose</c> y <c>exp</c> (~5 min); nunca rol ni permisos.
/// </summary>
public interface IMfaTokenService
{
    /// <summary>
    /// Emite un mfaToken para el usuario con el propósito indicado (p.ej. "mfa-setup" o
    /// "mfa-pending"), válido por una ventana corta (~5 min).
    /// </summary>
    string Emitir(Guid userId, string purpose);

    /// <summary>
    /// Valida firma, expiración y que el claim <c>purpose</c> coincida con el esperado.
    /// Devuelve el userId (claim <c>sub</c>) si el token es válido; <c>null</c> en cualquier
    /// otro caso (firma inválida, expirado, propósito distinto, malformado).
    /// </summary>
    Guid? Validar(string token, string purposeEsperado);
}
