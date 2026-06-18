namespace GymFlow.Domain.Entities;

/// <summary>
/// Código de recuperación de un solo uso para el segundo factor (MFA) de un empleado.
/// Se almacena hasheado; nunca en claro.
/// </summary>
public class CodigoRecuperacionMfa
{
    public Guid Id { get; private set; }
    public Guid EmpleadoId { get; private set; }
    public string CodigoHash { get; private set; } = string.Empty;
    public bool Usado { get; private set; }
    public DateTime? FechaUso { get; private set; }

    private CodigoRecuperacionMfa() { } // EF Core

    public CodigoRecuperacionMfa(Guid empleadoId, string codigoHash)
    {
        if (empleadoId == Guid.Empty)
            throw new ArgumentException("El empleado es requerido.", nameof(empleadoId));
        if (string.IsNullOrWhiteSpace(codigoHash))
            throw new ArgumentException("El hash del código es requerido.", nameof(codigoHash));

        Id = Guid.NewGuid();
        EmpleadoId = empleadoId;
        CodigoHash = codigoHash;
        Usado = false;
    }

    public void MarcarUsado(DateTime ahora)
    {
        Usado = true;
        FechaUso = ahora;
    }
}
