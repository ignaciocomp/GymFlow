using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public abstract class Usuario
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public string Correo { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Rol Rol { get; private set; }
    public bool EstaActivo { get; private set; } = true;
    public DateTime FechaCreacion { get; private set; }

    // N:M with Unidad
    public ICollection<UsuarioUnidad> UnidadesAsignadas { get; private set; } = new List<UsuarioUnidad>();

    protected Usuario() { } // EF Core

    protected Usuario(string nombre, string apellido, string correo, string passwordHash, Rol rol)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido : throw new ArgumentException("Apellido is required.", nameof(apellido));
        Correo = !string.IsNullOrWhiteSpace(correo) ? correo : throw new ArgumentException("Correo is required.", nameof(correo));
        PasswordHash = !string.IsNullOrWhiteSpace(passwordHash) ? passwordHash : throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        Rol = rol;
        EstaActivo = true;
        FechaCreacion = DateTime.UtcNow;
    }

    public void Desactivar()
    {
        EstaActivo = false;
    }

    public void Activar()
    {
        EstaActivo = true;
    }

    public void ActualizarDatosBase(string nombre, string apellido, string correo)
    {
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido : throw new ArgumentException("Apellido is required.", nameof(apellido));
        Correo = !string.IsNullOrWhiteSpace(correo) ? correo : throw new ArgumentException("Correo is required.", nameof(correo));
    }
}
