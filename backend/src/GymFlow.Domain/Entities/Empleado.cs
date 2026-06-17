namespace GymFlow.Domain.Entities;

public class Empleado : Usuario
{
    // Segundo factor (MFA TOTP) — obligatorio para empleados (IT5).
    private const int MaxIntentosFallidosMfa = 5;
    private const int MinutosBloqueoMfa = 15;

    public string? MfaSecret { get; private set; }
    public bool MfaHabilitado { get; private set; }
    public int MfaIntentosFallidos { get; private set; }
    public DateTime? MfaBloqueadoHasta { get; private set; }

    private Empleado() { } // EF Core

    public Empleado(string nombre, string apellido, string correo, string passwordHash, Guid rolId)
        : base(nombre, apellido, correo, passwordHash, rolId)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required for Empleado.", nameof(passwordHash));
    }

    public void CambiarRol(Guid nuevoRolId) => CambiarRolInterno(nuevoRolId);

    /// <summary>
    /// Guarda el secreto TOTP (ya cifrado) y habilita el segundo factor.
    /// </summary>
    public void ActivarMfa(string secretoProtegido)
    {
        if (string.IsNullOrWhiteSpace(secretoProtegido))
            throw new ArgumentException("El secreto MFA es requerido.", nameof(secretoProtegido));

        MfaSecret = secretoProtegido;
        MfaHabilitado = true;
    }

    /// <summary>
    /// Registra un intento de verificación fallido. Al alcanzar el máximo,
    /// bloquea el segundo factor durante 15 minutos.
    /// </summary>
    public void RegistrarIntentoFallidoMfa(DateTime ahora)
    {
        MfaIntentosFallidos++;

        if (MfaIntentosFallidos >= MaxIntentosFallidosMfa)
            MfaBloqueadoHasta = ahora.AddMinutes(MinutosBloqueoMfa);
    }

    /// <summary>
    /// Indica si el segundo factor está bloqueado en el instante dado.
    /// </summary>
    public bool EstaBloqueadoMfa(DateTime ahora) =>
        MfaBloqueadoHasta.HasValue && MfaBloqueadoHasta.Value > ahora;

    /// <summary>
    /// Limpia el contador de intentos fallidos y el bloqueo tras una verificación exitosa.
    /// </summary>
    public void RegistrarVerificacionExitosaMfa()
    {
        MfaIntentosFallidos = 0;
        MfaBloqueadoHasta = null;
    }

    /// <summary>
    /// Deshabilita el segundo factor y limpia todo su estado (reset por admin).
    /// </summary>
    public void ResetearMfa()
    {
        MfaHabilitado = false;
        MfaSecret = null;
        MfaIntentosFallidos = 0;
        MfaBloqueadoHasta = null;
    }
}
