using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionMapper
{
    public static InscripcionClaseDto ToDto(InscripcionClase i, Clase clase, int cuposOcupados, int? posicionListaEspera = null) =>
        new(i.Id,
            i.ClaseId,
            clase.Nombre,
            clase.Instructor,
            clase.UnidadId,
            clase.Unidad?.Nombre ?? "",
            clase.CapacidadMaxima,
            cuposOcupados,
            i.FechaInscripcion,
            i.EsListaEspera,
            posicionListaEspera);
}
