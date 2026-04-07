using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record CreateSocioRequest(
    string Nombre,
    string Apellido,
    string Correo,
    string? Telefono,
    TipoDocumento TipoDocumento,
    string? DocumentoIdentidad,
    DateTime? FechaNacimiento,
    List<UnidadAsignacionDto> Unidades,
    bool ConsentimientoInformado);
