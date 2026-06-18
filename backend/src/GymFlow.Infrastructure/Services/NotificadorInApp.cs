using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Crea notificaciones in-app en un contexto EF efímero PROPIO (vía IDbContextFactory),
/// no el GymFlowDbContext scoped compartido. Así su SaveChanges no flushea cambios de
/// negocio pendientes (p.ej. los RecordatorioCuota del job) y el commit es genuinamente
/// independiente. No maneja try/catch: el best-effort lo aplica el caller.
/// </summary>
public class NotificadorInApp : INotificadorInApp
{
    private readonly IDbContextFactory<GymFlowDbContext> _factory;

    public NotificadorInApp(IDbContextFactory<GymFlowDbContext> factory) => _factory = factory;

    public Task CrearAsync(Guid socioId, TipoNotificacion tipo, string titulo, string mensaje)
        => CrearParaVariosAsync(new[] { socioId }, tipo, titulo, mensaje);

    public async Task CrearParaVariosAsync(IEnumerable<Guid> socioIds, TipoNotificacion tipo, string titulo, string mensaje)
    {
        var ids = socioIds.ToList();
        if (ids.Count == 0) return;

        var notificaciones = ids.Select(socioId => new Notificacion(socioId, tipo, titulo, mensaje));

        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.Notificaciones.AddRange(notificaciones);
        await ctx.SaveChangesAsync();
    }
}
