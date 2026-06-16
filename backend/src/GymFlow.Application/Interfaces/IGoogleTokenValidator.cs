namespace GymFlow.Application.Interfaces;

/// <summary>
/// Valida un ID token de Google Identity Services y extrae el payload relevante.
/// Devuelve null si el token es inválido (firma, expiración o audience incorrectos).
/// </summary>
public interface IGoogleTokenValidator
{
    Task<GoogleTokenPayload?> ValidarAsync(string idToken);
}

public record GoogleTokenPayload(string Sub, string Email, bool EmailVerificado);
