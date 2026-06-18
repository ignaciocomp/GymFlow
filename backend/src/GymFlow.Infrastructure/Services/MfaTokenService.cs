using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Emite y valida el token intermedio de MFA (<c>mfaToken</c>) del login en dos pasos.
/// Se firma con <c>Mfa:TokenSigningKey</c> (clave DEDICADA, distinta de <c>Jwt:Key</c>) usando
/// HmacSha256 — idéntico patrón que el JWT de sesión. Como el <c>AddJwtBearer</c> global solo
/// conoce <c>Jwt:Key</c>, cualquier mfaToken presentado contra un endpoint normal es rechazado
/// (401) por firma inválida. El token lleva solo los claims <c>sub</c>, <c>purpose</c> y
/// <c>exp</c> (~5 min); nunca rol ni permisos.
/// </summary>
public class MfaTokenService : IMfaTokenService
{
    private const int MinutosVigencia = 5;

    private readonly byte[] _clave;

    public MfaTokenService(IConfiguration configuration)
    {
        var clave = configuration["Mfa:TokenSigningKey"];
        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException(
                "Falta la configuración 'Mfa:TokenSigningKey' para firmar el token de MFA.");
        }

        _clave = Encoding.UTF8.GetBytes(clave);
    }

    public string Emitir(Guid userId, string purpose)
    {
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("purpose", purpose)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(MinutosVigencia),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(_clave), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? Validar(string token, string purposeEsperado)
    {
        // MapInboundClaims=false para que el claim "sub" no se remapee a NameIdentifier
        // y podamos leerlo tal cual lo emitimos.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_clave),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var purpose = principal.FindFirst("purpose")?.Value;
            if (!string.Equals(purpose, purposeEsperado, StringComparison.Ordinal))
            {
                return null;
            }

            var sub = principal.FindFirst("sub")?.Value;
            if (Guid.TryParse(sub, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception)
        {
            // Firma inválida, expirado, malformado: fail-closed sin loguear el token.
            return null;
        }
    }
}
