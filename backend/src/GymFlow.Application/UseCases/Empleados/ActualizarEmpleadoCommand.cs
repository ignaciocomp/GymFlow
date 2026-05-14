using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class ActualizarEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IAuditLogger _auditLogger;

    public ActualizarEmpleadoCommand(
        IEmpleadoRepository empleadoRepository,
        IRolRepository rolRepository,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
        _auditLogger = auditLogger;
    }

    public async Task<EmpleadoDto> ExecuteAsync(Guid id, ActualizarEmpleadoRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido) || string.IsNullOrWhiteSpace(request.Correo))
            throw new ArgumentException("Nombre, apellido y correo son obligatorios.", nameof(request));

        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        if (await _empleadoRepository.ExisteCorreoAsync(request.Correo, id, ct))
            throw new InvalidOperationException("El correo ingresado ya está registrado por otro usuario.");

        var rol = await _rolRepository.GetByIdAsync(request.RolId, ct)
            ?? throw new ArgumentException($"El rol {request.RolId} no existe.", nameof(request));

        if (rol.Id == RolesSeed.SocioRolId)
            throw new InvalidOperationException("No se puede asignar el rol Socio a un empleado.");

        empleado.ActualizarDatosBase(request.Nombre, request.Apellido, request.Correo);
        empleado.CambiarRol(rol.Id);

        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Empleado", id,
            $"Se actualizó el empleado {empleado.Nombre} {empleado.Apellido} (rol {rol.Nombre})");

        return new EmpleadoDto(empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Correo,
            rol.Id, rol.Nombre, empleado.EstaActivo, empleado.FechaCreacion);
    }
}
