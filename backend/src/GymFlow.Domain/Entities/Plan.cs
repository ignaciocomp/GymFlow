namespace GymFlow.Domain.Entities;

public class Plan
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public decimal Precio { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public Guid UnidadId { get; private set; }
    public Unidad Unidad { get; private set; } = null!;
    public bool EstaActivo { get; private set; } = true;

    private Plan() { } // EF Core

    public Plan(string nombre, decimal precio, string descripcion, Guid unidadId)
    {
        Id = Guid.NewGuid();
        Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : throw new ArgumentException("Nombre is required.", nameof(nombre));
        Precio = precio >= 0 ? precio : throw new ArgumentException("Precio must be non-negative.", nameof(precio));
        Descripcion = descripcion ?? string.Empty;
        UnidadId = unidadId;
        EstaActivo = true;
    }

    public void Desactivar()
    {
        EstaActivo = false;
    }
}
