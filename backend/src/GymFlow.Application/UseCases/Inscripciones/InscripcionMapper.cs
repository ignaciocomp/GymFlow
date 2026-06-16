using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionMapper
{
    public static InscripcionClaseDto ToDto(InscripcionClase i, int cuposOcupados)
    {
        return ToDto(i, i.HorarioClase, cuposOcupados);
    }

    public static InscripcionClaseDto ToDto(InscripcionClase i, HorarioClase horario, int cuposOcupados)
    {
        var clase = horario.Clase;

        return new(
            i.Id,
            i.HorarioClaseId,
            horario.ClaseId,
            clase.Nombre,
            clase.Instructor,
            clase.UnidadId,
            clase.Unidad?.Nombre ?? "",
            horario.DiaSemana,
            horario.HoraInicio.ToString("HH:mm"),
            horario.HoraFin.ToString("HH:mm"),
            horario.Sala,
            clase.CapacidadMaxima,
            cuposOcupados,
            i.FechaInscripcion);
    }
}
