using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Planes;

public class UpdatePlanCommand
{
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public UpdatePlanCommand(IPlanRepository planRepository, IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task<PlanDto> ExecuteAsync(Guid id, UpdatePlanRequest request, Guid usuarioId, string usuarioNombre)
    {
        var plan = await _planRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el plan con ID {id}.");

        if (!plan.EstaActivo)
            throw new InvalidOperationException("No se puede editar un plan dado de baja.");

        plan.Actualizar(request.Nombre, request.Precio, request.Descripcion ?? "");

        await _planRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Plan", id,
            $"Se modificó el plan {request.Nombre}");

        return new PlanDto(plan.Id, plan.Nombre, plan.Precio, plan.Descripcion, plan.UnidadId, plan.Unidad?.Nombre ?? "", plan.EstaActivo);
    }
}
