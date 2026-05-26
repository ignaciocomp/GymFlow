using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class DarDeBajaEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IAuditLogger _auditLogger;

    public DarDeBajaEmpleadoCommand(IEmpleadoRepository empleadoRepository, IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (id == usuarioId)
            throw new InvalidOperationException("No podés darte de baja a vos mismo.");

        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        empleado.Desactivar();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Empleado", id,
            $"Se dio de baja al empleado {empleado.Nombre} {empleado.Apellido}");
    }
}
