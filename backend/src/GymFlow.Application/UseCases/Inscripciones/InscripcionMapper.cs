using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionMapper
{
    public static InscripcionClaseDto ToDto(InscripcionClase i, int cuposOcupados, int? posicionListaEspera = null)
    {
        return ToDto(i, i.HorarioClase, cuposOcupados, posicionListaEspera);
    }

    public static InscripcionClaseDto ToDto(InscripcionClase i, HorarioClase horario, int cuposOcupados, int? posicionListaEspera = null)
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
            i.FechaInscripcion,
            i.EsListaEspera,
            posicionListaEspera);
    }
}
