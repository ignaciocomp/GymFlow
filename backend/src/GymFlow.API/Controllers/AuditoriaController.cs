using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Auditoria;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly GetAuditoriaQuery _getAuditoriaQuery;

    public AuditoriaController(GetAuditoriaQuery getAuditoriaQuery)
    {
        _getAuditoriaQuery = getAuditoriaQuery;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditoriaDto>>> GetAll(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] TipoAccionAuditoria? tipoAccion,
        [FromQuery] Guid? entidadId)
    {
        if (desde.HasValue)
            desde = DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc);
        if (hasta.HasValue)
            hasta = DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc);

        var registros = await _getAuditoriaQuery.ExecuteAsync(desde, hasta, tipoAccion, entidadId);
        return Ok(registros);
    }
}
