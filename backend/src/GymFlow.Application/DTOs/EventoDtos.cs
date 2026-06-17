namespace GymFlow.Application.DTOs;

public record EventoDto(
    Guid Id,
    string Titulo,
    string Descripcion,
    DateTime Fecha,
    Guid UnidadId,
    string UnidadNombre,
    bool EstaActivo);
