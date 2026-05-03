using GymFlow.API.Authorization;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmpleadosController : ControllerBase
{
    private readonly GetEmpleadosQuery _getEmpleados;
    private readonly GetEmpleadoByIdQuery _getEmpleadoById;
    private readonly CrearEmpleadoCommand _crear;
    private readonly ActualizarEmpleadoCommand _actualizar;
    private readonly CambiarPasswordCommand _cambiarPassword;
    private readonly DarDeBajaEmpleadoCommand _darDeBaja;
    private readonly ReactivarEmpleadoCommand _reactivar;

    public EmpleadosController(
        GetEmpleadosQuery getEmpleados,
        GetEmpleadoByIdQuery getEmpleadoById,
        CrearEmpleadoCommand crear,
        ActualizarEmpleadoCommand actualizar,
        CambiarPasswordCommand cambiarPassword,
        DarDeBajaEmpleadoCommand darDeBaja,
        ReactivarEmpleadoCommand reactivar)
    {
        _getEmpleados = getEmpleados;
        _getEmpleadoById = getEmpleadoById;
        _crear = crear;
        _actualizar = actualizar;
        _cambiarPassword = cambiarPassword;
        _darDeBaja = darDeBaja;
        _reactivar = reactivar;
    }

    [HttpGet]
    [RequierePermiso(Modulo.Empleados, Operacion.Lectura)]
    public async Task<ActionResult<IReadOnlyList<EmpleadoDto>>> GetAll([FromQuery] bool? activo)
        => Ok(await _getEmpleados.ExecuteAsync(activo));

    [HttpGet("{id:guid}")]
    [RequierePermiso(Modulo.Empleados, Operacion.Lectura)]
    public async Task<ActionResult<EmpleadoDto>> GetById(Guid id)
    {
        try { return Ok(await _getEmpleadoById.ExecuteAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost]
    [RequierePermiso(Modulo.Empleados, Operacion.Escritura)]
    public async Task<ActionResult<EmpleadoDto>> Create([FromBody] CrearEmpleadoRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            var dto = await _crear.ExecuteAsync(request, uid, uname);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActualizarEmpleadoRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _actualizar.ExecuteAsync(id, request, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPatch("{id:guid}/password")]
    [RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]
    public async Task<IActionResult> CambiarPassword(Guid id, [FromBody] CambiarPasswordRequest request)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _cambiarPassword.ExecuteAsync(id, request.NuevaPassword, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [RequierePermiso(Modulo.Empleados, Operacion.Eliminacion)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _darDeBaja.ExecuteAsync(id, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPatch("{id:guid}/reactivar")]
    [RequierePermiso(Modulo.Empleados, Operacion.Modificacion)]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        try
        {
            var (uid, uname) = GetCurrentUser();
            await _reactivar.ExecuteAsync(id, uid, uname);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
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
