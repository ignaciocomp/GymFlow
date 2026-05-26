namespace GymFlow.Application.DTOs;

public record CrearEmpleadoRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string Password,
    Guid RolId);
