using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Auth.Mfa;

/// <summary>
/// Resultado de la activación del segundo factor: los 10 códigos de recuperación
/// en claro. Se devuelven UNA sola vez (después se guardan únicamente hasheados);
/// el empleado debe anotarlos en ese momento.
/// </summary>
public record ActivarMfaResultado(IReadOnlyList<string> CodigosRecuperacion);

/// <summary>
/// Activa el segundo factor del empleado tras validar el primer código TOTP.
/// Descifra el secreto guardado en el alta, valida el código; si es correcto
/// habilita el MFA, genera y persiste (hasheados) los códigos de recuperación y
/// los devuelve en claro. Si el código es inválido no habilita nada y lanza.
/// </summary>
public class ActivarMfaCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly ICodigoRecuperacionMfaRepository _codigosRepository;
    private readonly ITotpService _totpService;
    private readonly IMfaSecretProtector _secretProtector;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public ActivarMfaCommand(
        IEmpleadoRepository empleadoRepository,
        ICodigoRecuperacionMfaRepository codigosRepository,
        ITotpService totpService,
        IMfaSecretProtector secretProtector,
        IPasswordHasher passwordHasher,
        IAuditLogger auditLogger)
    {
        _empleadoRepository = empleadoRepository;
        _codigosRepository = codigosRepository;
        _totpService = totpService;
        _secretProtector = secretProtector;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task<ActivarMfaResultado> ActivarMfaAsync(Guid empleadoId, string codigo, CancellationToken ct = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(empleadoId, ct)
            ?? throw new KeyNotFoundException($"Empleado {empleadoId} no encontrado.");

        if (string.IsNullOrWhiteSpace(empleado.MfaSecret))
            throw new InvalidOperationException("No hay un alta de MFA pendiente para activar.");

        var secretoClaro = _secretProtector.Unprotect(empleado.MfaSecret);

        if (!_totpService.ValidarCodigo(secretoClaro, codigo))
            throw new UnauthorizedAccessException("Código incorrecto o expirado.");

        empleado.ActivarMfa(empleado.MfaSecret);

        var codigosClaro = _totpService.GenerarCodigosRecuperacion();
        var codigosEntidad = codigosClaro
            .Select(c => new CodigoRecuperacionMfa(empleadoId, _passwordHasher.Hash(c)))
            .ToList();

        await _codigosRepository.AgregarRangoAsync(codigosEntidad);
        await _codigosRepository.SaveChangesAsync();
        await _empleadoRepository.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(
            empleadoId, $"{empleado.Nombre} {empleado.Apellido}",
            TipoAccionAuditoria.MfaActivado, "Empleado", empleadoId,
            $"El empleado {empleado.Nombre} {empleado.Apellido} activó el segundo factor (MFA)");

        return new ActivarMfaResultado(codigosClaro);
    }
}
