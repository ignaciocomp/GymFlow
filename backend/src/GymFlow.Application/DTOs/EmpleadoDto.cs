namespace GymFlow.Application.DTOs;

public record EmpleadoDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Correo,
    Guid? RolId,
    string? RolNombre,
    bool EstaActivo,
    DateTime FechaCreacion);
