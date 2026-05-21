using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Horarios;

internal static class HorarioMapper
{
    public static HorarioClaseDto ToDto(HorarioClase h, int inscripcionesActivas) =>
        new(h.Id,
            h.ClaseId,
            h.Clase?.Nombre ?? "",
            h.Clase?.Instructor ?? "",
            h.Clase?.UnidadId ?? Guid.Empty,
            h.Clase?.Unidad?.Nombre ?? "",
            h.DiaSemana,
            h.HoraInicio.ToString("HH:mm"),
            h.HoraFin.ToString("HH:mm"),
            h.Sala,
            h.Clase?.CapacidadMaxima ?? 0,
            inscripcionesActivas);
}
