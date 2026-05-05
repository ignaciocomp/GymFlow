using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.DTOs;
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
    private readonly IPermisoCache _permisoCache;
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(
        IConfiguration configuration,
        IAuditLogger auditLogger,
        IPermisoCache permisoCache,
        IEmpleadoRepository empleadoRepository,
        ISocioRepository socioRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher)
    {
        _configuration = configuration;
        _auditLogger = auditLogger;
        _permisoCache = permisoCache;
        _empleadoRepository = empleadoRepository;
        _socioRepository = socioRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "El correo y la contraseña son obligatorios." });

        var empleado = await _empleadoRepository.GetByCorreoAsync(request.Correo);
        if (empleado != null && empleado.EstaActivo && empleado.RolId.HasValue &&
            !string.IsNullOrEmpty(empleado.PasswordHash) &&
            _passwordHasher.Verify(request.Password, empleado.PasswordHash))
        {
            var rolId = empleado.RolId.Value;
            var rol = await _rolRepository.GetByIdAsync(rolId);
            var rolNombre = rol?.Nombre ?? "—";

            var token = GenerateJwt(empleado.Id, empleado.Correo, rolId, rolNombre, empleado.Nombre, empleado.Apellido);
            var permisos = await _permisoCache.ObtenerPermisosAsync(rolId);
            var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

            await _auditLogger.LogAsync(
                empleado.Id, $"{empleado.Nombre} {empleado.Apellido}",
                TipoAccionAuditoria.InicioSesion, "Sesion", null,
                $"Inicio de sesión de {empleado.Nombre} {empleado.Apellido} ({rolNombre})");

            return Ok(new LoginResponse(token, empleado.Nombre, empleado.Apellido, empleado.Correo, rolNombre, permisosDto));
        }

        var socio = await _socioRepository.GetByCorreoAsync(request.Correo);
        if (socio != null && socio.EstaActivo && socio.RolId.HasValue &&
            !string.IsNullOrEmpty(socio.PasswordHash) &&
            _passwordHasher.Verify(request.Password, socio.PasswordHash))
        {
            var rolId = socio.RolId.Value;
            var rol = await _rolRepository.GetByIdAsync(rolId);
            var rolNombre = rol?.Nombre ?? "Socio";

            var token = GenerateJwt(socio.Id, socio.Correo, rolId, rolNombre, socio.Nombre, socio.Apellido);
            var permisos = await _permisoCache.ObtenerPermisosAsync(rolId);
            var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

            await _auditLogger.LogAsync(
                socio.Id, $"{socio.Nombre} {socio.Apellido}",
                TipoAccionAuditoria.InicioSesion, "Sesion", null,
                $"Inicio de sesión de {socio.Nombre} {socio.Apellido} ({rolNombre})");

            return Ok(new LoginResponse(token, socio.Nombre, socio.Apellido, socio.Correo, rolNombre, permisosDto));
        }

        return Unauthorized(new { error = "Correo o contraseña incorrectos." });
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

    private string GenerateJwt(Guid id, string correo, Guid rolId, string rolNombre, string nombre, string apellido)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "GymFlowDevSecretKey2026!SuperSecure");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, correo),
            new Claim("rolId", rolId.ToString()),
            new Claim("rolNombre", rolNombre),
            new Claim("nombre", nombre),
            new Claim("apellido", apellido)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Correo, string Password);
public record LoginResponse(string Token, string Nombre, string Apellido, string Correo, string RolNombre, IReadOnlyList<PermisoDto> Permisos);
