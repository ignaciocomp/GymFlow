using Google.Apis.Auth;
using GymFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Valida ID tokens de Google Identity Services usando Google.Apis.Auth
/// (verifica firma, expiración y audience contra nuestro Client ID).
/// </summary>
public class GoogleIdTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleIdTokenValidator> _logger;

    public GoogleIdTokenValidator(IConfiguration configuration, ILogger<GoogleIdTokenValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
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
        catch (InvalidJwtException)
        {
            // Token inválido (firma, expiración, audience, formato): caso esperado,
            // devolvemos null sin loguear nada para no filtrar el idToken.
            return null;
        }
        catch (Exception ex)
        {
            // Falla inesperada (config, red contra los certificados de Google):
            // fail-closed igual, pero dejamos rastro del tipo de error (nunca el token).
            _logger.LogWarning("Falló la validación del token de Google: {Tipo}: {Mensaje}",
                ex.GetType().Name, ex.Message);
            return null;
        }
    }
}
