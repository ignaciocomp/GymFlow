using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuditLogger _auditLogger;
    private readonly IPermisoCache _permisoCache;

    // Hardcoded users (Iteration 1) — apuntando al RolId de seed
    private static readonly List<HardcodedUser> Users = new()
    {
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001"), "admin@gymflow.com", "admin123", "Maurice", "Admin", RolSeed.AdminRolId, "Administrador"),
        new(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000003"), "socio@gymflow.com", "socio123", "María", "López", RolSeed.SocioRolId, "Socio")
    };

    public AuthController(IConfiguration configuration, IAuditLogger auditLogger, IPermisoCache permisoCache)
    {
        _configuration = configuration;
        _auditLogger = auditLogger;
        _permisoCache = permisoCache;
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
        var permisos = await _permisoCache.ObtenerPermisosAsync(user.RolId);
        var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

        await _auditLogger.LogAsync(
            user.Id, $"{user.Nombre} {user.Apellido}",
            TipoAccionAuditoria.InicioSesion, "Sesion", null,
            $"Inicio de sesión de {user.Nombre} {user.Apellido} ({user.RolNombre})");

        return Ok(new LoginResponse(token, user.Nombre, user.Apellido, user.Correo, user.RolNombre, permisosDto));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
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

            var rolId = Guid.Parse(principal.FindFirst("rolId")?.Value ?? Guid.Empty.ToString());
            var permisos = await _permisoCache.ObtenerPermisosAsync(rolId);
            var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

            return Ok(new
            {
                nombre = principal.FindFirst("nombre")?.Value,
                apellido = principal.FindFirst("apellido")?.Value,
                correo = principal.FindFirst(ClaimTypes.Email)?.Value,
                rolNombre = principal.FindFirst("rolNombre")?.Value,
                permisos = permisosDto
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
            new Claim("rolId", user.RolId.ToString()),
            new Claim("rolNombre", user.RolNombre),
            new Claim("nombre", user.Nombre),
            new Claim("apellido", user.Apellido)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private record HardcodedUser(Guid Id, string Correo, string Password, string Nombre, string Apellido, Guid RolId, string RolNombre);
}

public record LoginRequest(string Correo, string Password);
public record LoginResponse(string Token, string Nombre, string Apellido, string Correo, string RolNombre, IReadOnlyList<PermisoDto> Permisos);
