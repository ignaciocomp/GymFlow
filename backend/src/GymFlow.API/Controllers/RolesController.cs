using GymFlow.API.Authorization;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly GetRolesQuery _getRoles;
    private readonly GetRolByIdQuery _getRolById;
    private readonly CrearRolCommand _crearRol;
    private readonly ActualizarRolCommand _actualizarRol;
    private readonly EliminarRolCommand _eliminarRol;

    public RolesController(
        GetRolesQuery getRoles, GetRolByIdQuery getRolById,
        CrearRolCommand crearRol, ActualizarRolCommand actualizarRol, EliminarRolCommand eliminarRol)
    {
        _getRoles = getRoles;
        _getRolById = getRolById;
        _crearRol = crearRol;
        _actualizarRol = actualizarRol;
        _eliminarRol = eliminarRol;
    }

    // Reutiliza el permiso Auditoria como proxy de "gestión administrativa".
    // En seed, solo el Administrador lo tiene. Si más adelante se necesita aislar,
    // agregar Modulo.Roles al enum siguiendo la convención de docs/agent_Context.md.

    [HttpGet]
    [RequierePermiso(Modulo.Auditoria, Operacion.Lectura)]
    public async Task<ActionResult<IReadOnlyList<RolDto>>> GetAll() =>
        Ok(await _getRoles.ExecuteAsync());

    [HttpGet("{id:guid}")]
    [RequierePermiso(Modulo.Auditoria, Operacion.Lectura)]
    public async Task<ActionResult<RolDto>> GetById(Guid id)
    {
        try { return Ok(await _getRolById.ExecuteAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost]
    [RequierePermiso(Modulo.Auditoria, Operacion.Escritura)]
    public async Task<ActionResult<RolDto>> Create([FromBody] CrearRolRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            var dto = await _crearRol.ExecuteAsync(request, uid, uname);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Modulo.Auditoria, Operacion.Modificacion)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActualizarRolRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _actualizarRol.ExecuteAsync(id, request, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Auditoria, Operacion.Eliminacion)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _eliminarRol.ExecuteAsync(id, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    private (Guid Id, string Nombre) GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var nombre = User.FindFirst("nombre")?.Value ?? "";
        var apellido = User.FindFirst("apellido")?.Value ?? "";
        var fullName = $"{nombre} {apellido}".Trim();
        return (userId, string.IsNullOrWhiteSpace(fullName) ? "Sistema" : fullName);
    }
}
