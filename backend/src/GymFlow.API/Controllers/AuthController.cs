using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth;
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
    private readonly LoginConGoogleCommand _loginConGoogleCommand;

    public AuthController(
        IConfiguration configuration,
        IAuditLogger auditLogger,
        IPermisoCache permisoCache,
        IEmpleadoRepository empleadoRepository,
        ISocioRepository socioRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        LoginConGoogleCommand loginConGoogleCommand)
    {
        _configuration = configuration;
        _auditLogger = auditLogger;
        _permisoCache = permisoCache;
        _empleadoRepository = empleadoRepository;
        _socioRepository = socioRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _loginConGoogleCommand = loginConGoogleCommand;
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

            var unidadIds = socio.UnidadesAsignadas.Select(u => u.UnidadId).ToList();
            return Ok(new LoginResponse(token, socio.Nombre, socio.Apellido, socio.Correo, rolNombre, permisosDto, unidadIds));
        }

        return Unauthorized(new { error = "Correo o contraseña incorrectos." });
    }

    [HttpPost("google")]
    public async Task<IActionResult> LoginConGoogle([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { error = "El token de Google es obligatorio." });

        try
        {
            var socio = await _loginConGoogleCommand.ExecuteAsync(request.IdToken);

            var rolId = socio.RolId!.Value;
            var rol = await _rolRepository.GetByIdAsync(rolId);
            var rolNombre = rol?.Nombre ?? "Socio";

            var token = GenerateJwt(socio.Id, socio.Correo, rolId, rolNombre, socio.Nombre, socio.Apellido);
            var permisos = await _permisoCache.ObtenerPermisosAsync(rolId);
            var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

            await _auditLogger.LogAsync(
                socio.Id, $"{socio.Nombre} {socio.Apellido}",
                TipoAccionAuditoria.InicioSesion, "Sesion", null,
                $"Inicio de sesión con Google de {socio.Nombre} {socio.Apellido} ({rolNombre})");

            var unidadIds = socio.UnidadesAsignadas.Select(u => u.UnidadId).ToList();
            return Ok(new LoginResponse(token, socio.Nombre, socio.Apellido, socio.Correo, rolNombre, permisosDto, unidadIds));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
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

            var correo = principal.FindFirst(ClaimTypes.Email)?.Value;
            List<Guid>? unidadIds = null;
            var socio = await _socioRepository.GetByCorreoAsync(correo ?? "");
            if (socio != null)
                unidadIds = socio.UnidadesAsignadas.Select(u => u.UnidadId).ToList();

            return Ok(new
            {
                nombre = principal.FindFirst("nombre")?.Value,
                apellido = principal.FindFirst("apellido")?.Value,
                correo,
                rolNombre = principal.FindFirst("rolNombre")?.Value,
                permisos = permisosDto,
                unidadIds
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
public record GoogleLoginRequest(string IdToken);
public record LoginResponse(string Token, string Nombre, string Apellido, string Correo, string RolNombre, IReadOnlyList<PermisoDto> Permisos, IReadOnlyList<Guid>? UnidadIds = null);
