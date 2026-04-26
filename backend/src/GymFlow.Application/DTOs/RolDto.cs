namespace GymFlow.Application.DTOs;

public record RolDto(
    Guid Id,
    string Nombre,
    bool EsSistema,
    DateTime FechaCreacion,
    IReadOnlyList<PermisoDto> Permisos);
