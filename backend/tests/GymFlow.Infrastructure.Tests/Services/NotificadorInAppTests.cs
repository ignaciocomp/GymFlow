using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Services;

public class NotificadorInAppTests
{
    // Factory de test: todos los contextos comparten la misma base InMemory (mismo nombre),
    // así lo persistido por el contexto efímero del notificador es visible al verificar.
    private sealed class TestDbContextFactory : IDbContextFactory<GymFlowDbContext>
    {
        private readonly DbContextOptions<GymFlowDbContext> _options;

        public TestDbContextFactory(string dbName)
        {
            _options = new DbContextOptionsBuilder<GymFlowDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
        }

        public GymFlowDbContext CreateDbContext() => new(_options);

        public Task<GymFlowDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new GymFlowDbContext(_options));
    }

    private static (NotificadorInApp sut, IDbContextFactory<GymFlowDbContext> factory) CrearSut()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        return (new NotificadorInApp(factory), factory);
    }

    [Fact]
    public async Task CrearAsync_PersisteUnaNotificacion()
    {
        var (sut, factory) = CrearSut();
        var socioId = Guid.NewGuid();

        await sut.CrearAsync(socioId, TipoNotificacion.RecordatorioCuota, "Cuota vencida", "Tu cuota de mayo está pendiente.");

        await using var ctx = await factory.CreateDbContextAsync();
        var notifs = await ctx.Notificaciones.ToListAsync();

        Assert.Single(notifs);
        var notif = notifs[0];
        Assert.Equal(socioId, notif.SocioId);
        Assert.Equal(TipoNotificacion.RecordatorioCuota, notif.Tipo);
        Assert.Equal("Cuota vencida", notif.Titulo);
        Assert.Equal("Tu cuota de mayo está pendiente.", notif.Mensaje);
        Assert.False(notif.Leida);
    }

    [Fact]
    public async Task CrearParaVariosAsync_PersisteUnaNotificacionPorSocio()
    {
        var (sut, factory) = CrearSut();
        var socioIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        await sut.CrearParaVariosAsync(socioIds, TipoNotificacion.EventoNuevo, "Nuevo evento", "Se publicó un evento en tu unidad.");

        await using var ctx = await factory.CreateDbContextAsync();
        var notifs = await ctx.Notificaciones.ToListAsync();

        Assert.Equal(3, notifs.Count);
        Assert.All(notifs, n => Assert.Equal(TipoNotificacion.EventoNuevo, n.Tipo));
        Assert.All(notifs, n => Assert.Equal("Nuevo evento", n.Titulo));
        Assert.Equal(socioIds.OrderBy(x => x), notifs.Select(n => n.SocioId).OrderBy(x => x));
    }

    [Fact]
    public async Task CrearParaVariosAsync_SinSocios_NoPersisteNada()
    {
        var (sut, factory) = CrearSut();

        await sut.CrearParaVariosAsync(Array.Empty<Guid>(), TipoNotificacion.EventoNuevo, "Nuevo evento", "Mensaje.");

        await using var ctx = await factory.CreateDbContextAsync();
        Assert.Empty(await ctx.Notificaciones.ToListAsync());
    }
}
