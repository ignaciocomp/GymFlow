namespace GymFlow.Application.DTOs;

public record InscripcionClaseDto(
    Guid Id,
    Guid ClaseId,
    string ClaseNombre,
    string Instructor,
    Guid UnidadId,
    string UnidadNombre,
    int CapacidadMaxima,
    int InscripcionesActivas,
    DateTime FechaInscripcion,
    bool EnListaEspera,
    int? PosicionListaEspera);

public record InscribirSocioRequest(Guid ClaseId);
