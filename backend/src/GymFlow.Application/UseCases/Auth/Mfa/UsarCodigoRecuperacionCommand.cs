using GymFlow.Application.Exceptions;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Auth.Mfa;

/// <summary>
/// Permite entrar usando un código de recuperación de un solo uso cuando el
/// empleado perdió su dispositivo. Evalúa el bloqueo al entrar; si el código
/// matchea uno activo (hash BCrypt) lo consume, resetea el contador y devuelve el
/// empleado para emitir el JWT; si no matchea, suma un intento fallido al mismo
/// contador de bloqueo y lanza.
/// </summary>
public class UsarCodigoRecuperacionCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly ICodigoRecuperacionMfaRepository _codigosRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public UsarCodigoRecuperacionCommand(
        IEmpleadoRepository empleadoRepository,
        ICodigoRecuperacionMfaRepository codigosRepository,
        IPasswordHasher passwordHasher,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _codigosRepository = codigosRepository;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task<Empleado> UsarCodigoRecuperacionAsync(Guid empleadoId, string codigo, CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;

        var empleado = await _empleadoRepository.GetByIdAsync(empleadoId, ct)
            ?? throw new KeyNotFoundException($"Empleado {empleadoId} no encontrado.");

        if (empleado.EstaBloqueadoMfa(ahora))
            throw new MfaBloqueadoException();

        var activos = await _codigosRepository.GetActivosPorEmpleadoAsync(empleadoId);
        var match = activos.FirstOrDefault(c => _passwordHasher.Verify(codigo, c.CodigoHash));

        if (match is null)
        {
            empleado.RegistrarIntentoFallidoMfa(ahora);
            await _empleadoRepository.SaveChangesAsync(ct);

            if (empleado.EstaBloqueadoMfa(ahora))
            {
                await _auditLogger.LogAsync(
                    empleadoId, $"{empleado.Nombre} {empleado.Apellido}",
                    TipoAccionAuditoria.MfaBloqueado, "Empleado", empleadoId,
                    $"Se bloqueó el segundo factor de {empleado.Nombre} {empleado.Apellido} por superar los intentos permitidos");
            }

            throw new UnauthorizedAccessException("Código incorrecto o expirado.");
        }

        match.MarcarUsado(ahora);
        await _codigosRepository.SaveChangesAsync();

        empleado.RegistrarVerificacionExitosaMfa();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            empleadoId, $"{empleado.Nombre} {empleado.Apellido}",
            TipoAccionAuditoria.MfaCodigoRecuperacionUsado, "Empleado", empleadoId,
            $"El empleado {empleado.Nombre} {empleado.Apellido} usó un código de recuperación del segundo factor (MFA)");

        return empleado;
    }
}
