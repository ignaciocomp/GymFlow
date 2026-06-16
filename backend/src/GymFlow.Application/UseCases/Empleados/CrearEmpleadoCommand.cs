using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class CrearEmpleadoCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;

    public CrearEmpleadoCommand(
        IEmpleadoRepository empleadoRepository,
        IRolRepository rolRepository,
        IPasswordHasher passwordHasher,
        IAuditLogger auditLogger,
        IEmailService emailService)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
        _emailService = emailService;
    }

    public async Task<EmpleadoDto> ExecuteAsync(CrearEmpleadoRequest request, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Apellido))
            throw new ArgumentException("El apellido es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Correo))
            throw new ArgumentException("El correo es obligatorio.", nameof(request));

        if (await _empleadoRepository.ExisteCorreoAsync(request.Correo, null, ct))
            throw new InvalidOperationException("El correo ingresado ya está registrado.");

        var rol = await _rolRepository.GetByIdAsync(request.RolId, ct)
            ?? throw new ArgumentException($"El rol {request.RolId} no existe.", nameof(request));

        if (rol.Id == RolesSeed.SocioRolId)
            throw new InvalidOperationException("No se puede asignar el rol Socio a un empleado.");

        var passwordTemporal = GeneradorPassword.Generar();
        var hash = _passwordHasher.Hash(passwordTemporal);
        var empleado = new Empleado(request.Nombre, request.Apellido, request.Correo, hash, rol.Id);

        await _empleadoRepository.AddAsync(empleado, ct);
        await _empleadoRepository.SaveChangesAsync(ct);

        var (asunto, cuerpo) = EmpleadoEmailTemplates.Bienvenida(empleado, passwordTemporal, rol.Nombre);
        var emailRes = await _emailService.EnviarAsync(empleado.Correo, asunto, cuerpo);

        var estadoEmail = emailRes.Exitoso
            ? "se envió el correo de bienvenida con las credenciales temporales"
            : $"falló el envío del correo de bienvenida ({emailRes.Error})";

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Empleado", empleado.Id,
            $"Se creó el empleado {empleado.Nombre} {empleado.Apellido} ({rol.Nombre}); {estadoEmail}");

        return new EmpleadoDto(empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Correo,
            rol.Id, rol.Nombre, empleado.EstaActivo, empleado.FechaCreacion);
    }
}
