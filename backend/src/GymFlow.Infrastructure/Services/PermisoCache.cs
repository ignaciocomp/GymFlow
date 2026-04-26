using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GymFlow.Infrastructure.Services;

public class PermisoCache : IPermisoCache
{
    private readonly IMemoryCache _cache;
    private readonly GymFlowDbContext _db;
    private static readonly TimeSpan TTL = TimeSpan.FromMinutes(30);

    public PermisoCache(IMemoryCache cache, GymFlowDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public async Task<bool> TienePermisoAsync(Guid rolId, Modulo modulo, Operacion operacion, CancellationToken ct = default)
    {
        var permisos = await ObtenerPermisosAsync(rolId, ct);
        return permisos.Any(p => p.Modulo == modulo && p.Operacion == operacion);
    }

    public async Task<IReadOnlyList<(Modulo Modulo, Operacion Operacion)>> ObtenerPermisosAsync(Guid rolId, CancellationToken ct = default)
    {
        var key = CacheKey(rolId);
        if (_cache.TryGetValue<IReadOnlyList<(Modulo, Operacion)>>(key, out var cached) && cached is not null)
            return cached;

        var permisos = await _db.RolPermisos
            .Where(rp => rp.RolId == rolId)
            .Select(rp => new { rp.Permiso.Modulo, rp.Permiso.Operacion })
            .ToListAsync(ct);

        var result = permisos.Select(p => (p.Modulo, p.Operacion)).ToList();
        _cache.Set(key, (IReadOnlyList<(Modulo, Operacion)>)result, TTL);
        return result;
    }

    public void Invalidar(Guid rolId) => _cache.Remove(CacheKey(rolId));

    private static string CacheKey(Guid rolId) => $"permisos:rol:{rolId}";
}
