using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Unidades;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnidadesController : ControllerBase
{
    private readonly GetUnidadesQuery _getUnidadesQuery;

    public UnidadesController(GetUnidadesQuery getUnidadesQuery)
    {
        _getUnidadesQuery = getUnidadesQuery;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UnidadDto>>> GetAll()
    {
        var unidades = await _getUnidadesQuery.ExecuteAsync();
        return Ok(unidades);
    }
}
