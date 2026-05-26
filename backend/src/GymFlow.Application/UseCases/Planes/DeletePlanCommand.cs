using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Planes;

public class DeletePlanCommand
{
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public DeletePlanCommand(IPlanRepository planRepository, IAuditLogger auditLogger)
    {
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var plan = await _planRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el plan con ID {id}.");

        if (!plan.EstaActivo)
            throw new InvalidOperationException("El plan ya está dado de baja.");

        if (await _planRepository.ExisteSocioConPlanAsync(id))
            throw new InvalidOperationException("El plan tiene socios asignados. Reasígnelos antes de darlo de baja.");

        plan.Desactivar();
        await _planRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Plan", id,
            $"Se dio de baja el plan {plan.Nombre}");
    }
}
