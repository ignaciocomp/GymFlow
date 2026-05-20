namespace GymFlow.Application.DTOs;

public record ClaseDto(
    Guid Id,
    string Nombre,
    string Descripcion,
    int CapacidadMaxima,
    int DuracionMinutos,
    string Instructor,
    Guid UnidadId,
    string UnidadNombre,
    bool EstaActivo,
    int InscripcionesActivas);

public record CreateClaseRequest(
    string Nombre,
    string? Descripcion,
    int CapacidadMaxima,
    int DuracionMinutos,
    string Instructor,
    Guid UnidadId);

public record UpdateClaseRequest(
    string Nombre,
    string? Descripcion,
    int CapacidadMaxima,
    int DuracionMinutos,
    string Instructor);
