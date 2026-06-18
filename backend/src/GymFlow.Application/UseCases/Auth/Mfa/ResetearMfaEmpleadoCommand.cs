using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Auth.Mfa;

/// <summary>
/// Resetea el segundo factor de un empleado por decisión de un administrador
/// (p. ej. cuando perdió el dispositivo y los códigos de recuperación). Limpia el
/// estado MFA del empleado, elimina sus códigos de recuperación y audita la acción
/// a nombre del admin. Un admin no puede resetearse a sí mismo.
/// </summary>
public class ResetearMfaEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly ICodigoRecuperacionMfaRepository _codigosRepository;
    private readonly IAuditLogger _auditLogger;

    public ResetearMfaEmpleadoCommand(
        IEmpleadoRepository empleadoRepository,
        ICodigoRecuperacionMfaRepository codigosRepository,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _codigosRepository = codigosRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid empleadoId, Guid adminId, string adminNombre, CancellationToken ct = default)
    {
        if (empleadoId == adminId)
            throw new InvalidOperationException("No podés resetear tu propio segundo factor.");

        var empleado = await _empleadoRepository.GetByIdAsync(empleadoId, ct)
            ?? throw new KeyNotFoundException($"Empleado {empleadoId} no encontrado.");

        empleado.ResetearMfa();

        await _codigosRepository.EliminarPorEmpleadoAsync(empleadoId);
        await _codigosRepository.SaveChangesAsync();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            adminId, adminNombre,
            TipoAccionAuditoria.MfaReseteadoPorAdmin, "Empleado", empleadoId,
            $"Se reseteó el segundo factor (MFA) del empleado {empleado.Nombre} {empleado.Apellido}");
    }
}
