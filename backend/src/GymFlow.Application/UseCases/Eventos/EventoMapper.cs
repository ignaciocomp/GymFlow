using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Eventos;

public static class EventoMapper
{
    public static EventoDto ToDto(Evento evento) =>
        new(evento.Id, evento.Titulo, evento.Descripcion, evento.Fecha,
            evento.UnidadId, evento.Unidad?.Nombre ?? "", evento.EstaActivo);
}
