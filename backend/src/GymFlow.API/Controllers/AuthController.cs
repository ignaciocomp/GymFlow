using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.DTOs;
using GymFlow.Application.Exceptions;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth;
using GymFlow.Application.UseCases.Auth.Mfa;
using GymFlow.Domain.Entities;
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
    private readonly IMfaTokenService _mfaTokenService;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IniciarMfaSetupCommand _iniciarMfaSetupCommand;
    private readonly ActivarMfaCommand _activarMfaCommand;
    private readonly VerificarMfaCommand _verificarMfaCommand;
    private readonly UsarCodigoRecuperacionCommand _usarCodigoRecuperacionCommand;

    // Propósitos del mfaToken intermedio (deben coincidir con los que emite el login).
    private const string PurposeSetup = "mfa-setup";
    private const string PurposePending = "mfa-pending";

    public AuthController(
        IConfiguration configuration,
        IAuditLogger auditLogger,
        IPermisoCache permisoCache,
        IEmpleadoRepository empleadoRepository,
        ISocioRepository socioRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        LoginConGoogleCommand loginConGoogleCommand,
        IMfaTokenService mfaTokenService,
        IQrCodeGenerator qrCodeGenerator,
        IniciarMfaSetupCommand iniciarMfaSetupCommand,
        ActivarMfaCommand activarMfaCommand,
        VerificarMfaCommand verificarMfaCommand,
        UsarCodigoRecuperacionCommand usarCodigoRecuperacionCommand)
    {
        _configuration = configuration;
        _auditLogger = auditLogger;
        _permisoCache = permisoCache;
        _empleadoRepository = empleadoRepository;
        _socioRepository = socioRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _loginConGoogleCommand = loginConGoogleCommand;
        _mfaTokenService = mfaTokenService;
        _qrCodeGenerator = qrCodeGenerator;
        _iniciarMfaSetupCommand = iniciarMfaSetupCommand;
        _activarMfaCommand = activarMfaCommand;
        _verificarMfaCommand = verificarMfaCommand;
        _usarCodigoRecuperacionCommand = usarCodigoRecuperacionCommand;
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
            // Empleado: la contraseña es solo el primer paso. No emitimos la sesión acá;
            // devolvemos un mfaToken intermedio y dejamos que el segundo factor complete el login.
            var setupRequerido = !empleado.MfaHabilitado;
            var purpose = setupRequerido ? PurposeSetup : PurposePending;
            var mfaToken = _mfaTokenService.Emitir(empleado.Id, purpose);

            // Con MFA, el empleado no recibe sesión acá: completa el segundo factor y la sesión
            // (con sus unidadIds, para el rol Dueño) se emite en CrearSesionEmpleadoAsync.
            return Ok(new LoginResultado(RequiereMfa: true, SetupRequerido: setupRequerido, MfaToken: mfaToken, Sesion: null));
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
            var sesion = new LoginResponse(token, socio.Nombre, socio.Apellido, socio.Correo, rolNombre, permisosDto, unidadIds);
            return Ok(new LoginResultado(RequiereMfa: false, SetupRequerido: false, MfaToken: null, Sesion: sesion));
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

    [HttpPost("mfa/setup")]
    public async Task<IActionResult> MfaSetup()
    {
        var empleadoId = ValidarMfaToken(PurposeSetup);
        if (empleadoId is null)
            return Unauthorized(new { error = "Token de MFA inválido o expirado." });

        try
        {
            var resultado = await _iniciarMfaSetupCommand.IniciarMfaSetupAsync(empleadoId.Value);
            var qrDataUri = _qrCodeGenerator.GenerarPngDataUri(resultado.UriOtpauth);

            return Ok(new MfaSetupResponse(resultado.UriOtpauth, qrDataUri, resultado.SecretoBase32));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("mfa/activate")]
    public async Task<IActionResult> MfaActivate([FromBody] MfaActivarRequest request)
    {
        var empleadoId = ValidarMfaToken(PurposeSetup);
        if (empleadoId is null)
            return Unauthorized(new { error = "Token de MFA inválido o expirado." });

        if (string.IsNullOrWhiteSpace(request.Codigo))
            return BadRequest(new { error = "El código es obligatorio." });

        try
        {
            var resultado = await _activarMfaCommand.ActivarMfaAsync(empleadoId.Value, request.Codigo);
            var empleado = await _empleadoRepository.GetByIdAsync(empleadoId.Value)
                ?? throw new KeyNotFoundException($"Empleado {empleadoId} no encontrado.");

            var sesion = await CrearSesionEmpleadoAsync(empleado);
            return Ok(new MfaActivarResponse(sesion, resultado.CodigosRecuperacion));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Código incorrecto o expirado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("mfa/verify")]
    public async Task<IActionResult> MfaVerify([FromBody] MfaVerificarRequest request)
    {
        var empleadoId = ValidarMfaToken(PurposePending);
        if (empleadoId is null)
            return Unauthorized(new { error = "Token de MFA inválido o expirado." });

        if (string.IsNullOrWhiteSpace(request.Codigo))
            return BadRequest(new { error = "El código es obligatorio." });

        try
        {
            var empleado = await _verificarMfaCommand.VerificarMfaAsync(empleadoId.Value, request.Codigo);
            var sesion = await CrearSesionEmpleadoAsync(empleado);
            return Ok(sesion);
        }
        catch (MfaBloqueadoException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Código incorrecto o expirado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("mfa/recovery")]
    public async Task<IActionResult> MfaRecovery([FromBody] MfaRecoveryRequest request)
    {
        var empleadoId = ValidarMfaToken(PurposePending);
        if (empleadoId is null)
            return Unauthorized(new { error = "Token de MFA inválido o expirado." });

        if (string.IsNullOrWhiteSpace(request.Codigo))
            return BadRequest(new { error = "El código es obligatorio." });

        try
        {
            var empleado = await _usarCodigoRecuperacionCommand.UsarCodigoRecuperacionAsync(empleadoId.Value, request.Codigo);
            var sesion = await CrearSesionEmpleadoAsync(empleado);
            return Ok(sesion);
        }
        catch (MfaBloqueadoException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Código incorrecto o expirado." });
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
            {
                unidadIds = socio.UnidadesAsignadas.Select(u => u.UnidadId).ToList();
            }
            else
            {
                var empleado = await _empleadoRepository.GetByCorreoAsync(correo ?? "");
                if (empleado != null)
                    unidadIds = empleado.UnidadesAsignadas.Select(u => u.UnidadId).ToList();
            }

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

    /// <summary>
    /// Extrae el mfaToken del header <c>Authorization: Bearer</c> (mismo patrón que <see cref="Me"/>)
    /// y lo valida contra el propósito esperado. Devuelve el id del empleado o null si no es válido.
    /// </summary>
    private Guid? ValidarMfaToken(string purposeEsperado)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader["Bearer ".Length..];
        return _mfaTokenService.Validar(token, purposeEsperado);
    }

    /// <summary>
    /// Arma la <see cref="LoginResponse"/> de sesión de un empleado tras superar el segundo factor,
    /// con los mismos claims y permisos que el login normal (paridad total).
    /// </summary>
    private async Task<LoginResponse> CrearSesionEmpleadoAsync(Empleado empleado)
    {
        var rolId = empleado.RolId!.Value;
        var rol = await _rolRepository.GetByIdAsync(rolId);
        var rolNombre = rol?.Nombre ?? "—";

        var token = GenerateJwt(empleado.Id, empleado.Correo, rolId, rolNombre, empleado.Nombre, empleado.Apellido);
        var permisos = await _permisoCache.ObtenerPermisosAsync(rolId);
        var permisosDto = permisos.Select(p => new PermisoDto(Guid.Empty, p.Modulo, p.Operacion)).ToList();

        await _auditLogger.LogAsync(
            empleado.Id, $"{empleado.Nombre} {empleado.Apellido}",
            TipoAccionAuditoria.InicioSesion, "Sesion", null,
            $"Inicio de sesión de {empleado.Nombre} {empleado.Apellido} ({rolNombre})");

        // Las unidades asignadas del empleado (rol Dueño) viajan en la sesión, igual que el socio.
        var unidadIds = empleado.UnidadesAsignadas.Select(u => u.UnidadId).ToList();
        return new LoginResponse(token, empleado.Nombre, empleado.Apellido, empleado.Correo, rolNombre, permisosDto, unidadIds);
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

/// <summary>
/// Resultado del paso 1 del login. Para empleados: <c>RequiereMfa=true</c> con un <c>MfaToken</c>
/// intermedio (y <c>SetupRequerido</c> según tenga o no el segundo factor ya activado). Para
/// socios/legacy: <c>RequiereMfa=false</c> y la <c>Sesion</c> con el JWT ya emitido.
/// </summary>
public record LoginResultado(bool RequiereMfa, bool SetupRequerido, string? MfaToken, LoginResponse? Sesion);

public record MfaActivarRequest(string Codigo);
public record MfaVerificarRequest(string Codigo);
public record MfaRecoveryRequest(string Codigo);

/// <summary>Datos del alta de MFA: URI otpauth, QR como data URI PNG y la clave manual (base32).</summary>
public record MfaSetupResponse(string UriOtpauth, string QrDataUri, string ClaveManual);

/// <summary>Respuesta de la activación: la sesión emitida y los códigos de recuperación (una sola vez).</summary>
public record MfaActivarResponse(LoginResponse Sesion, IReadOnlyList<string> CodigosRecuperacion);
