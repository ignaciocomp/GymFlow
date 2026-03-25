namespace GymFlow.Application.DTOs;

public record UpdateSocioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    Guid? PlanId,
    List<Guid> UnidadIds);
