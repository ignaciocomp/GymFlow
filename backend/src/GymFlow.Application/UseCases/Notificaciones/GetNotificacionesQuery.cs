using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Notificaciones;

public class GetNotificacionesQuery
{
    private readonly INotificacionRepository _repository;

    public GetNotificacionesQuery(INotificacionRepository repository) => _repository = repository;

    public async Task<IEnumerable<NotificacionDto>> ExecuteAsync(Guid socioId, bool soloNoLeidas, int take)
    {
        var notificaciones = await _repository.GetBySocioAsync(socioId, soloNoLeidas, take);
        return notificaciones.Select(MapToDto);
    }

    internal static NotificacionDto MapToDto(Notificacion n) => new(
        Id: n.Id,
        Tipo: n.Tipo,
        Titulo: n.Titulo,
        Mensaje: n.Mensaje,
        Leida: n.Leida,
        FechaCreacion: n.FechaCreacion);
}
