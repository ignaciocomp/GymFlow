using Google.Apis.Auth;
using GymFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Valida ID tokens de Google Identity Services usando Google.Apis.Auth
/// (verifica firma, expiración y audience contra nuestro Client ID).
/// </summary>
public class GoogleIdTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;

    public GoogleIdTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleTokenPayload?> ValidarAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleTokenPayload(payload.Subject, payload.Email, payload.EmailVerified);
        }
        catch
        {
            // Token inválido (firma, expiración, audience, formato) o falla al validar:
            // devolvemos null sin loguear nada para no filtrar el idToken.
            return null;
        }
    }
}
