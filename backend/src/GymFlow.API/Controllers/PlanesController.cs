using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Planes;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanesController : ControllerBase
{
    private readonly GetPlanesQuery _getPlanesQuery;

    public PlanesController(GetPlanesQuery getPlanesQuery)
    {
        _getPlanesQuery = getPlanesQuery;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanDto>>> GetAll([FromQuery] Guid? unidadId)
    {
        var planes = await _getPlanesQuery.ExecuteAsync(unidadId);
        return Ok(planes);
    }
}
