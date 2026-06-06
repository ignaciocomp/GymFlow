using GymFlow.API.Authorization;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Unidades;
using GymFlow.Domain.Enums;
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

    [HttpGet]
    [RequierePermiso(Modulo.Unidades, Operacion.Lectura)]
    public async Task<ActionResult<IEnumerable<UnidadDto>>> GetAll()
    {
        var unidades = await _getUnidadesQuery.ExecuteAsync();
        return Ok(unidades);
    }
}
