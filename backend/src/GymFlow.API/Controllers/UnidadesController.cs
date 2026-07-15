using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Unidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnidadesController : ControllerBase
{
    private readonly GetUnidadesQuery _getUnidadesQuery;

    public UnidadesController(GetUnidadesQuery getUnidadesQuery)
    {
        _getUnidadesQuery = getUnidadesQuery;
    }

    // Solo autenticación ([Authorize] del controller): el portal del socio necesita listar
    // las sedes para el filtro de Horarios, y el rol Socio no tiene permisos de módulo.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UnidadDto>>> GetAll()
    {
        var unidades = await _getUnidadesQuery.ExecuteAsync();
        return Ok(unidades);
    }
}
