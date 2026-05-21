using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record HorarioClaseDto(
    Guid Id,
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
    int InscripcionesActivas);

public record CreateHorarioClaseRequest(
    Guid ClaseId,
    DiaSemana DiaSemana,
    string HoraInicio,
    string HoraFin,
    string? Sala);

public record UpdateHorarioClaseRequest(
    DiaSemana DiaSemana,
    string HoraInicio,
    string HoraFin,
    string? Sala);
