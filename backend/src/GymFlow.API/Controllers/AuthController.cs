using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuditLogger _auditLogger;

    // Hardcoded users for development (Iteration 1)
    private static readonly List<HardcodedUser> Users =
    [
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001"), "admin@gymflow.com", "admin123", "Maurice", "Admin", "Admin"),
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002"), "profesor@gymflow.com", "profesor123", "Carlos", "García", "Profesor"),
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000003"), "socio@gymflow.com", "socio123", "María", "López", "Socio")
    ];

    public AuthController(IConfiguration configuration, IAuditLogger auditLogger)
    {
        _configuration = configuration;
        _auditLogger = auditLogger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "El correo y la contraseña son obligatorios." });

        var user = Users.FirstOrDefault(u =>
            u.Correo.Equals(request.Correo, StringComparison.OrdinalIgnoreCase) &&
            u.Password == request.Password);

        if (user == null)
            return Unauthorized(new { error = "Correo o contraseña incorrectos." });

        var token = GenerateJwt(user);

        await _auditLogger.LogAsync(
            user.Id,
            $"{user.Nombre} {user.Apellido}",
            TipoAccionAuditoria.InicioSesion,
            "Sesion",
            null,
            $"Inicio de sesión del {user.Rol.ToLower()} {user.Nombre} {user.Apellido}");

        return Ok(new LoginResponse(
            Token: token,
            Nombre: user.Nombre,
            Apellido: user.Apellido,
            Correo: user.Correo,
            Rol: user.Rol));
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized();

        try
        {
            var token = authHeader["Bearer ".Length..];
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "GymFlowDevSecretKey2026!SuperSecure");
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Ok(new
            {
                nombre = principal.FindFirst("nombre")?.Value,
                apellido = principal.FindFirst("apellido")?.Value,
                correo = principal.FindFirst(ClaimTypes.Email)?.Value,
                rol = principal.FindFirst(ClaimTypes.Role)?.Value
            });
        }
        catch
        {
            return Unauthorized(new { error = "Token inválido o expirado." });
        }
    }

    private string GenerateJwt(HardcodedUser user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "GymFlowDevSecretKey2026!SuperSecure");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Correo),
            new Claim(ClaimTypes.Role, user.Rol),
            new Claim("nombre", user.Nombre),
            new Claim("apellido", user.Apellido)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private record HardcodedUser(Guid Id, string Correo, string Password, string Nombre, string Apellido, string Rol);
}

public record LoginRequest(string Correo, string Password);
public record LoginResponse(string Token, string Nombre, string Apellido, string Correo, string Rol);
