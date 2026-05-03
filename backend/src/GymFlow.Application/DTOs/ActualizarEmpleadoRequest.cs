namespace GymFlow.Application.DTOs;

public record ActualizarEmpleadoRequest(
    string Nombre,
    string Apellido,
    string Correo,
    Guid RolId);
