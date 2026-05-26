using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Planes;

public class CreatePlanCommand
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IAuditLogger _auditLogger;

    public CreatePlanCommand(IPlanRepository planRepository, IUnidadRepository unidadRepository, IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _unidadRepository = unidadRepository;
        _auditLogger = auditLogger;
    }

    public async Task<PlanDto> ExecuteAsync(CreatePlanRequest request, Guid usuarioId, string usuarioNombre)
    {
        var unidad = await _unidadRepository.GetByIdAsync(request.UnidadId)
            ?? throw new ArgumentException("La unidad seleccionada no existe.");

        var plan = new Plan(request.Nombre, request.Precio, request.Descripcion ?? "", request.UnidadId);

        await _planRepository.AddAsync(plan);
        await _planRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Plan", plan.Id,
            $"Se creó el plan {request.Nombre} para {unidad.Nombre}");

        return new PlanDto(plan.Id, plan.Nombre, plan.Precio, plan.Descripcion, plan.UnidadId, unidad.Nombre, plan.EstaActivo);
    }
}
