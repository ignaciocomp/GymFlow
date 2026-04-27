using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Planes;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanesController : ControllerBase
{
    private readonly GetPlanesQuery _getPlanesQuery;
    private readonly GetPlanByIdQuery _getPlanByIdQuery;
    private readonly CreatePlanCommand _createPlanCommand;
    private readonly UpdatePlanCommand _updatePlanCommand;
    private readonly DeletePlanCommand _deletePlanCommand;
    private readonly ReactivatePlanCommand _reactivatePlanCommand;

    public PlanesController(
        GetPlanesQuery getPlanesQuery,
        GetPlanByIdQuery getPlanByIdQuery,
        CreatePlanCommand createPlanCommand,
        UpdatePlanCommand updatePlanCommand,
        DeletePlanCommand deletePlanCommand,
        ReactivatePlanCommand reactivatePlanCommand)
    {
        _getPlanesQuery = getPlanesQuery;
        _getPlanByIdQuery = getPlanByIdQuery;
        _createPlanCommand = createPlanCommand;
        _updatePlanCommand = updatePlanCommand;
        _deletePlanCommand = deletePlanCommand;
        _reactivatePlanCommand = reactivatePlanCommand;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanDto>>> GetAll(
        [FromQuery] Guid? unidadId,
        [FromQuery] bool includeInactive = false)
    {
        var planes = await _getPlanesQuery.ExecuteAsync(unidadId, includeInactive);
        return Ok(planes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlanDto>> GetById(Guid id)
    {
        try
        {
            var plan = await _getPlanByIdQuery.ExecuteAsync(id);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create([FromBody] CreatePlanRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var plan = await _createPlanCommand.ExecuteAsync(request, userId, userName);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlanDto>> Update(Guid id, [FromBody] UpdatePlanRequest request)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var plan = await _updatePlanCommand.ExecuteAsync(id, request, userId, userName);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPatch("{id:guid}/reactivar")]
    public async Task<ActionResult<PlanDto>> Reactivate(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            var plan = await _reactivatePlanCommand.ExecuteAsync(id, userId, userName);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (userId, userName) = GetCurrentUser();
            await _deletePlanCommand.ExecuteAsync(id, userId, userName);
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
