using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Empleados;

public class CambiarPasswordCommand
{
    private const int MinPasswordLength = 8;

    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public CambiarPasswordCommand(IEmpleadoRepository empleadoRepository, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, string nuevaPassword, Guid usuarioId, string usuarioNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < MinPasswordLength)
            throw new ArgumentException($"La contraseña debe tener al menos {MinPasswordLength} caracteres.", nameof(nuevaPassword));

        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");

        empleado.EstablecerPasswordHash(_passwordHasher.Hash(nuevaPassword));
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Empleado", id,
            $"Se cambió la contraseña del empleado {empleado.Nombre} {empleado.Apellido}");
    }
}
