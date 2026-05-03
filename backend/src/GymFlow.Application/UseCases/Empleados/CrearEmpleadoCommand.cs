using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class CrearEmpleadoCommand
{
    private const int MinPasswordLength = 8;

    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public CrearEmpleadoCommand(
        IEmpleadoRepository empleadoRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task<EmpleadoDto> ExecuteAsync(CrearEmpleadoRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Apellido))
            throw new ArgumentException("El apellido es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Correo))
            throw new ArgumentException("El correo es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinPasswordLength)
            throw new ArgumentException($"La contraseña debe tener al menos {MinPasswordLength} caracteres.", nameof(request));

        if (await _empleadoRepository.ExisteCorreoAsync(request.Correo, null, ct))
            throw new InvalidOperationException("El correo ingresado ya está registrado.");

        var rol = await _rolRepository.GetByIdAsync(request.RolId, ct)
            ?? throw new ArgumentException($"El rol {request.RolId} no existe.", nameof(request));

        if (rol.Id == RolesSeed.SocioRolId)
            throw new InvalidOperationException("No se puede asignar el rol Socio a un empleado.");

        var hash = _passwordHasher.Hash(request.Password);
        var empleado = new Empleado(request.Nombre, request.Apellido, request.Correo, hash, rol.Id);

        await _empleadoRepository.AddAsync(empleado, ct);
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Empleado", empleado.Id,
            $"Se creó el empleado {empleado.Nombre} {empleado.Apellido} ({rol.Nombre})");

        return new EmpleadoDto(empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Correo,
            rol.Id, rol.Nombre, empleado.EstaActivo, empleado.FechaCreacion);
    }
}
