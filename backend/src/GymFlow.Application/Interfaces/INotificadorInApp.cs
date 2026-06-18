using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

/// <summary>
/// Crea notificaciones in-app para los socios. Best-effort: el caller la invoca
/// SIEMPRE después del SaveChanges de negocio y envuelta en try/catch, de modo que
/// si la creación falla, la operación de negocio igual queda confirmada.
/// La implementación persiste en un contexto efímero propio (commit aislado).
/// </summary>
public interface INotificadorInApp
{
    Task CrearAsync(Guid socioId, TipoNotificacion tipo, string titulo, string mensaje);

    Task CrearParaVariosAsync(IEnumerable<Guid> socioIds, TipoNotificacion tipo, string titulo, string mensaje);
}
