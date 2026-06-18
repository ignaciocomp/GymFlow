using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Auth.Mfa;

/// <summary>
/// Datos del alta de MFA que se devuelven al empleado para configurar su app
/// autenticadora: el secreto en claro (base32) para la clave manual / QR y el
/// URI <c>otpauth://</c>. Los códigos de recuperación NO se entregan acá: se
/// generan recién al activar el MFA (validando el primer código).
/// </summary>
public record IniciarMfaSetupResultado(string SecretoBase32, string UriOtpauth);

/// <summary>
/// Paso de alta del segundo factor. Genera un secreto TOTP, lo cifra y lo
/// persiste en el empleado dejando <c>MfaHabilitado=false</c> (el MFA recién se
/// habilita al activar). Devuelve el secreto en claro y el URI para el QR.
/// </summary>
public class IniciarMfaSetupCommand
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly ITotpService _totpService;
    private readonly IMfaSecretProtector _secretProtector;

    public IniciarMfaSetupCommand(
        IEmpleadoRepository empleadoRepository,
        ITotpService totpService,
        IMfaSecretProtector secretProtector)
    {
        _empleadoRepository = empleadoRepository;
        _totpService = totpService;
        _secretProtector = secretProtector;
    }

    public async Task<IniciarMfaSetupResultado> IniciarMfaSetupAsync(Guid empleadoId, CancellationToken ct = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(empleadoId, ct)
            ?? throw new KeyNotFoundException($"Empleado {empleadoId} no encontrado.");

        var secreto = _totpService.GenerarSecreto();
        var secretoProtegido = _secretProtector.Protect(secreto);

        empleado.PrepararSecretoMfa(secretoProtegido);
        await _empleadoRepository.SaveChangesAsync(ct);

        var uri = _totpService.GenerarUriOtpauth(secreto, empleado.Correo);
        return new IniciarMfaSetupResultado(secreto, uri);
    }
}
