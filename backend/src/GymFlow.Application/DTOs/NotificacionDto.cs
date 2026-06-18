using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record NotificacionDto(
    Guid Id,
    TipoNotificacion Tipo,
    string Titulo,
    string Mensaje,
    bool Leida,
    DateTime FechaCreacion);
