using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Permiso
{
    public Guid Id { get; private set; }
    public Modulo Modulo { get; private set; }
    public Operacion Operacion { get; private set; }

    private Permiso() { } // EF Core

    public Permiso(Modulo modulo, Operacion operacion)
    {
        Id = Guid.NewGuid();
        Modulo = modulo;
        Operacion = operacion;
    }

    // Constructor para seed data con Id explícito
    public Permiso(Guid id, Modulo modulo, Operacion operacion)
    {
        Id = id;
        Modulo = modulo;
        Operacion = operacion;
    }
}
