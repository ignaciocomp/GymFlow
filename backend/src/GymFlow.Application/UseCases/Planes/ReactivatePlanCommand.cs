using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Planes;

public class ReactivatePlanCommand
{
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public ReactivatePlanCommand(IPlanRepository planRepository, IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task<PlanDto> ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var plan = await _planRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el plan con ID {id}.");

        if (plan.EstaActivo)
            throw new InvalidOperationException("El plan ya está activo.");

        plan.Reactivar();
        await _planRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Reactivacion, "Plan", id,
            $"Se reactivó el plan {plan.Nombre}");

        return new PlanDto(plan.Id, plan.Nombre, plan.Precio, plan.Descripcion, plan.UnidadId, plan.Unidad?.Nombre ?? "", plan.EstaActivo);
    }
}
