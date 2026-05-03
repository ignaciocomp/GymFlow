using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class ReactivarEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IAuditLogger _auditLogger;

    public ReactivarEmpleadoCommand(IEmpleadoRepository empleadoRepository, IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        empleado.Activar();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Empleado", id,
            $"Se reactivó al empleado {empleado.Nombre} {empleado.Apellido}");
    }
}
