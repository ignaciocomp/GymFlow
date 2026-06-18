using GymFlow.Application.Exceptions;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Auth.Mfa;

/// <summary>
/// Verifica el código TOTP de 6 dígitos en los logins siguientes al alta. Evalúa
/// el bloqueo anti-fuerza-bruta al entrar; ante un código correcto resetea el
/// contador y devuelve el empleado para que la API emita el JWT de sesión; ante un
/// código incorrecto suma un intento fallido (y bloquea al quinto) y lanza.
/// </summary>
public class VerificarMfaCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly ITotpService _totpService;
    private readonly IMfaSecretProtector _secretProtector;
    private readonly IAuditLogger _auditLogger;

    public VerificarMfaCommand(
        IEmpleadoRepository empleadoRepository,
        ITotpService totpService,
        IMfaSecretProtector secretProtector,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _totpService = totpService;
        _secretProtector = secretProtector;
        _auditLogger = auditLogger;
    }

    public async Task<Empleado> VerificarMfaAsync(Guid empleadoId, string codigo, CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;

        var empleado = await _empleadoRepository.GetByIdAsync(empleadoId, ct)
            ?? throw new KeyNotFoundException($"Empleado {empleadoId} no encontrado.");

        if (empleado.EstaBloqueadoMfa(ahora))
            throw new MfaBloqueadoException();

        if (string.IsNullOrWhiteSpace(empleado.MfaSecret) || !empleado.MfaHabilitado)
            throw new InvalidOperationException("El empleado no tiene el segundo factor activado.");

        var secretoClaro = _secretProtector.Unprotect(empleado.MfaSecret);

        if (!_totpService.ValidarCodigo(secretoClaro, codigo))
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

        empleado.RegistrarVerificacionExitosaMfa();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            empleadoId, $"{empleado.Nombre} {empleado.Apellido}",
            TipoAccionAuditoria.MfaVerificado, "Empleado", empleadoId,
            $"El empleado {empleado.Nombre} {empleado.Apellido} verificó el segundo factor (MFA)");

        return empleado;
    }
}
