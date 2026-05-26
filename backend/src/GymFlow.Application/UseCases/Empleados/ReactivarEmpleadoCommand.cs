using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class ReactivarEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IAuditLogger _auditLogger;

    public ReactivarEmpleadoCommand(
        IEmpleadoRepository empleadoRepository,
        IRolRepository rolRepository,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
        _auditLogger = auditLogger;
    }

    public async Task<EmpleadoDto> ExecuteAsync(Guid id, Guid? nuevoRolId, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        if (empleado.EstaActivo)
            throw new InvalidOperationException("El empleado ya se encuentra activo.");

        if (!empleado.RolId.HasValue)
        {
            if (!nuevoRolId.HasValue)
                throw new InvalidOperationException("El rol del empleado fue eliminado. Debe seleccionar un nuevo rol para reactivarlo.");

            var rol = await _rolRepository.GetByIdAsync(nuevoRolId.Value, ct)
                ?? throw new ArgumentException($"El rol seleccionado no existe.", nameof(nuevoRolId));

            if (rol.Id == RolesSeed.SocioRolId)
                throw new InvalidOperationException("No se puede asignar el rol Socio a un empleado.");

            empleado.CambiarRol(nuevoRolId.Value);
        }

        empleado.Activar();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Empleado", id,
            $"Se reactivó al empleado {empleado.Nombre} {empleado.Apellido}");

        var rolActual = empleado.RolId.HasValue
            ? await _rolRepository.GetByIdAsync(empleado.RolId.Value, ct)
            : null;

        return new EmpleadoDto(empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Correo,
            rolActual?.Id, rolActual?.Nombre, empleado.EstaActivo, empleado.FechaCreacion);
    }
}
