using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Permisos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // cualquier usuario autenticado puede leer el catálogo
public class PermisosController : ControllerBase
{
    private readonly GetPermisosQuery _getPermisos;

    public PermisosController(GetPermisosQuery getPermisos) => _getPermisos = getPermisos;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermisoDto>>> GetAll() =>
        Ok(await _getPermisos.ExecuteAsync());
}
