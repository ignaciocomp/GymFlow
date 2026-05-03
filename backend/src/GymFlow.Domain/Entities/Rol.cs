namespace GymFlow.Domain.Entities;

public class Rol
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public bool EsSistema { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    public ICollection<RolPermiso> Permisos { get; private set; } = new List<RolPermiso>();

    private Rol() { } // EF Core

    public Rol(string nombre, bool esSistema = false)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        EsSistema = esSistema;
        FechaCreacion = DateTime.UtcNow;
    }

    // Constructor para seed con Id explícito
    public Rol(Guid id, string nombre, bool esSistema, DateTime fechaCreacion)
    {
        Id = id;
        Nombre = nombre;
        EsSistema = esSistema;
        FechaCreacion = fechaCreacion;
    }

    public void Renombrar(string nuevoNombre)
    {
        if (EsSistema)
            throw new InvalidOperationException("No se puede renombrar un rol del sistema.");

        Nombre = !string.IsNullOrWhiteSpace(nuevoNombre)
            ? nuevoNombre
            : throw new ArgumentException("Nombre is required.", nameof(nuevoNombre));
    }

    public void ReemplazarPermisos(IEnumerable<Guid> permisoIds)
    {
        if (EsSistema)
            throw new InvalidOperationException("No se pueden modificar los permisos de un rol del sistema.");

        Permisos.Clear();
        foreach (var pid in permisoIds.Distinct())
        {
            Permisos.Add(new RolPermiso(Id, pid));
        }
    }
}
