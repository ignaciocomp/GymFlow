using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace GymFlow.Infrastructure.Tests.Services;

public class MfaTokenServiceTests
{
    // Clave dedicada del mfaToken (UTF-8 ≥32 chars), distinta de Jwt:Key.
    private const string ClaveMfa = "GymFlowMfaTokenSigningKey2026!!Dedicada";
    private const string ClaveJwt = "GymFlowDevSecretKey2026!SuperSecure!!";

    private static MfaTokenService CrearServicio()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Mfa:TokenSigningKey"]).Returns(ClaveMfa);
        return new MfaTokenService(config.Object);
    }

    [Fact]
    public void EmitirYValidar_RoundTrip()
    {
        var sut = CrearServicio();
        var userId = Guid.NewGuid();

        var token = sut.Emitir(userId, "mfa-pending");
        var resultado = sut.Validar(token, "mfa-pending");

        Assert.Equal(userId, resultado);
    }

    [Fact]
    public void Validar_ConPurposeDistinto_Falla()
    {
        // Un token de mfa-setup no debe validar contra mfa-pending.
        var sut = CrearServicio();
        var userId = Guid.NewGuid();

        var token = sut.Emitir(userId, "mfa-setup");
        var resultado = sut.Validar(token, "mfa-pending");

        Assert.Null(resultado);
    }

    [Fact]
    public void Validar_ConOtraClave_Falla()
    {
        // Un token firmado con Jwt:Key (no con la clave de MFA) no valida:
        // la firma es distinta. Esto garantiza que el pipeline JWT global y el
        // servicio de MFA no se acepten tokens cruzados.
        var sut = CrearServicio();
        var userId = Guid.NewGuid();

        var tokenForaneo = ForjarToken(ClaveJwt, userId, "mfa-pending", DateTime.UtcNow.AddMinutes(5));
        var resultado = sut.Validar(tokenForaneo, "mfa-pending");

        Assert.Null(resultado);
    }

    [Fact]
    public void Validar_Expirado_Falla()
    {
        // Token firmado con la clave correcta pero ya expirado → null.
        var sut = CrearServicio();
        var userId = Guid.NewGuid();

        var tokenExpirado = ForjarToken(ClaveMfa, userId, "mfa-pending", DateTime.UtcNow.AddMinutes(-5));
        var resultado = sut.Validar(tokenExpirado, "mfa-pending");

        Assert.Null(resultado);
    }

    private static string ForjarToken(string clave, Guid userId, string purpose, DateTime expira)
    {
        var key = Encoding.UTF8.GetBytes(clave);
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("purpose", purpose)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expira,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
