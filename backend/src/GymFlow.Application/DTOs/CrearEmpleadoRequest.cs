namespace GymFlow.Application.DTOs;

public record CrearEmpleadoRequest(
    string Nombre,
    string Apellido,
    string Correo,
    Guid RolId,
    Guid[]? UnidadIds = null);
