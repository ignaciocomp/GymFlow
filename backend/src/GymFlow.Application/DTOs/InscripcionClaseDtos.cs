using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record InscripcionClaseDto(
    Guid Id,
    Guid HorarioClaseId,
    Guid ClaseId,
    string ClaseNombre,
    string Instructor,
    Guid UnidadId,
    string UnidadNombre,
    DiaSemana DiaSemana,
    string HoraInicio,
    string HoraFin,
    string? Sala,
    int CapacidadMaxima,
    int InscripcionesActivas,
    DateTime FechaInscripcion);

public record InscribirSocioRequest(Guid HorarioClaseId);
