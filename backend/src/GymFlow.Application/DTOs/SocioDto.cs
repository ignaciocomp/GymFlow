using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record SocioDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    TipoDocumento TipoDocumento,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    DateTime FechaAlta,
    bool EstaActivo,
    Guid? PlanId,
    string? PlanNombre,
    List<UnidadDto> Unidades);
