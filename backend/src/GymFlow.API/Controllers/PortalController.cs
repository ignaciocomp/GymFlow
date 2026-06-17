using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Application.UseCases.Portal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortalController : ControllerBase
{
    private readonly GetSocioPerfilQuery _getPerfilQuery;
    private readonly SolicitarModificacionCommand _solicitarModificacionCommand;
    private readonly SolicitarBajaCommand _solicitarBajaCommand;
    private readonly GetEventosPortalQuery _getEventosPortalQuery;
    private readonly IConfiguration _configuration;

    public PortalController(
        GetSocioPerfilQuery getPerfilQuery,
        SolicitarModificacionCommand solicitarModificacionCommand,
        SolicitarBajaCommand solicitarBajaCommand,
        GetEventosPortalQuery getEventosPortalQuery,
        IConfiguration configuration)
    {
        _getPerfilQuery = getPerfilQuery;
        _solicitarModificacionCommand = solicitarModificacionCommand;
        _solicitarBajaCommand = solicitarBajaCommand;
        _getEventosPortalQuery = getEventosPortalQuery;
        _configuration = configuration;
    }

    /// <summary>
    /// RF-05: Obtiene el perfil del socio autenticado
    /// </summary>
    [HttpGet("perfil")]
    public async Task<ActionResult<SocioDto>> GetPerfil()
    {
        var claims = ExtractClaims();
        if (claims == null) return Unauthorized(new { error = "Token inválido o expirado." });

        var correo = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(correo)) return Unauthorized(new { error = "No se pudo identificar al usuario." });

        try
        {
            var perfil = await _getPerfilQuery.ExecuteAsync(correo);
            return Ok(perfil);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// RF-05 / RNF-09b: El socio solicita modificación de sus datos personales
    /// </summary>
    [HttpPost("solicitar-modificacion")]
    public async Task<IActionResult> SolicitarModificacion([FromBody] SolicitarModificacionRequest request)
    {
        var claims = ExtractClaims();
        if (claims == null) return Unauthorized(new { error = "Token inválido o expirado." });

        var (correo, userId, userName) = GetCurrentUser(claims);

        try
        {
            await _solicitarModificacionCommand.ExecuteAsync(correo, request.Detalle, userId, userName);
            return Ok(new { mensaje = "Tu solicitud de modificación fue registrada. El equipo se contactará contigo a la brevedad." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// RF-05 / RNF-09b: El socio solicita la baja de su cuenta
    /// </summary>
    [HttpPost("solicitar-baja")]
    public async Task<IActionResult> SolicitarBaja([FromBody] SolicitarBajaRequest? request)
    {
        var claims = ExtractClaims();
        if (claims == null) return Unauthorized(new { error = "Token inválido o expirado." });

        var (correo, userId, userName) = GetCurrentUser(claims);

        try
        {
            await _solicitarBajaCommand.ExecuteAsync(correo, request?.Motivo, userId, userName);
            return Ok(new { mensaje = "Tu solicitud de baja fue registrada. El equipo procesará tu solicitud en los próximos días." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// RF-15: Próximos eventos activos de las sedes del socio autenticado
    /// </summary>
    [HttpGet("eventos")]
    public async Task<ActionResult<IEnumerable<EventoDto>>> GetEventos()
    {
        var claims = ExtractClaims();
        if (claims == null) return Unauthorized(new { error = "Token inválido o expirado." });

        var correo = claims.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(correo)) return Unauthorized(new { error = "No se pudo identificar al usuario." });

        try
        {
            var eventos = await _getEventosPortalQuery.ExecuteAsync(correo);
            return Ok(eventos);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private ClaimsPrincipal? ExtractClaims()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;

        try
        {
            var token = authHeader["Bearer ".Length..];
            var keyString = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key not configured");
            var key = Encoding.UTF8.GetBytes(keyString);
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static (string Correo, Guid UserId, string UserName) GetCurrentUser(ClaimsPrincipal claims)
    {
        var correo = claims.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var userId = Guid.TryParse(claims.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        var nombre = claims.FindFirst("nombre")?.Value ?? "";
        var apellido = claims.FindFirst("apellido")?.Value ?? "";
        var fullName = $"{nombre} {apellido}".Trim();
        return (correo, userId, string.IsNullOrWhiteSpace(fullName) ? correo : fullName);
    }
}
