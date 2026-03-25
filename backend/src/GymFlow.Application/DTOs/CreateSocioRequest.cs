namespace GymFlow.Application.DTOs;

public record CreateSocioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    Guid? PlanId,
    List<Guid> UnidadIds,
    bool ConsentimientoInformado);
