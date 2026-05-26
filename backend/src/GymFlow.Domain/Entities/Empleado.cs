namespace GymFlow.Domain.Entities;

public class Empleado : Usuario
{
    private Empleado() { } // EF Core

    public Empleado(string nombre, string apellido, string correo, string passwordHash, Guid rolId)
        : base(nombre, apellido, correo, passwordHash, rolId)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required for Empleado.", nameof(passwordHash));
    }

    public void CambiarRol(Guid nuevoRolId) => CambiarRolInterno(nuevoRolId);
}
