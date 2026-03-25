namespace GymFlow.Domain.Entities;

public class Unidad
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Direccion { get; private set; } = string.Empty;

    private Unidad() { } // EF Core

    public Unidad(string nombre, string direccion)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Direccion = !string.IsNullOrWhiteSpace(direccion) ? direccion : throw new ArgumentException("Direccion is required.", nameof(direccion));
    }

    public void Actualizar(string nombre, string direccion)
    {
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Direccion = !string.IsNullOrWhiteSpace(direccion) ? direccion : throw new ArgumentException("Direccion is required.", nameof(direccion));
    }
}
