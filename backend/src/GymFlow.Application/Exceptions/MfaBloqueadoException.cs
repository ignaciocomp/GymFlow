namespace GymFlow.Application.Exceptions;

/// <summary>
/// Se lanza cuando el segundo factor de un empleado está bloqueado por superar el
/// máximo de intentos fallidos. La API la mapea a 429 (Too Many Requests).
/// </summary>
public class MfaBloqueadoException : Exception
{
    public MfaBloqueadoException()
        : base("Demasiados intentos. Probá de nuevo en unos minutos.")
    {
    }

    public MfaBloqueadoException(string message) : base(message)
    {
    }
}
