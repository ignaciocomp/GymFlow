namespace GymFlow.Application.DTOs;

public record AuditoriaDto(
    Guid Id,
    Guid UsuarioId,
    string UsuarioNombre,
    string TipoAccion,
    string EntidadAfectada,
    Guid? EntidadId,
    string Descripcion,
    string? DetallesCambios,
    DateTime FechaHora);
