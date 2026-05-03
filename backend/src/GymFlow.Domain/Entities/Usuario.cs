namespace GymFlow.Domain.Entities;

public abstract class Usuario
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public string Correo { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public Guid RolId { get; private set; }
    public Rol Rol { get; private set; } = null!;
    public bool EstaActivo { get; private set; } = true;
    public DateTime FechaCreacion { get; private set; }

    public ICollection<UsuarioUnidad> UnidadesAsignadas { get; private set; } = new List<UsuarioUnidad>();

    protected Usuario() { } // EF Core

    protected Usuario(string nombre, string apellido, string correo, string? passwordHash, Guid rolId)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido : throw new ArgumentException("Apellido is required.", nameof(apellido));
        Correo = !string.IsNullOrWhiteSpace(correo) ? correo : throw new ArgumentException("Correo is required.", nameof(correo));
        PasswordHash = passwordHash; // nullable: Empleado lo setea, Socio lo deja null hasta OAuth (It.5)
        RolId = rolId != Guid.Empty ? rolId : throw new ArgumentException("RolId is required.", nameof(rolId));
        EstaActivo = true;
        FechaCreacion = DateTime.UtcNow;
    }

    public void EstablecerPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    protected void CambiarRolInterno(Guid nuevoRolId)
    {
        if (nuevoRolId == Guid.Empty)
            throw new ArgumentException("RolId is required.", nameof(nuevoRolId));
        RolId = nuevoRolId;
    }

    public void Desactivar() => EstaActivo = false;
    public void Activar() => EstaActivo = true;

    public void ActualizarDatosBase(string nombre, string apellido, string correo)
    {
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido : throw new ArgumentException("Apellido is required.", nameof(apellido));
        Correo = !string.IsNullOrWhiteSpace(correo) ? correo : throw new ArgumentException("Correo is required.", nameof(correo));
    }
}
