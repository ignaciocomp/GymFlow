using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Repositories;

public class PagoRepositoryTests
{
    private static GymFlowDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GymFlowDbContext(options);
    }

    private static Cuota CrearCuota(Guid socioId) =>
        new(socioId, Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 1500m, DateTime.UtcNow);

    [Fact]
    public async Task AddAsync_ThenGetById_DevuelveElPago()
    {
        using var ctx = CrearContexto();
        var repo = new PagoRepository(ctx);
        var socioId = Guid.NewGuid();
        var pago = new Pago(Guid.NewGuid(), socioId, 1500m, "pref-1");

        await repo.AddAsync(pago);
        await repo.SaveChangesAsync();

        var encontrado = await repo.GetByIdAsync(pago.Id);
        Assert.NotNull(encontrado);
        Assert.Equal(pago.Id, encontrado!.Id);
        Assert.Equal(socioId, encontrado.SocioId);
        Assert.Equal("pref-1", encontrado.MpPreferenceId);
    }

    [Fact]
    public async Task GetByIdAsync_Inexistente_DevuelveNull()
    {
        using var ctx = CrearContexto();
        var repo = new PagoRepository(ctx);

        var encontrado = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(encontrado);
    }

    [Fact]
    public async Task GetByExternalReferenceAsync_UsaElIdComoReferencia()
    {
        using var ctx = CrearContexto();
        var repo = new PagoRepository(ctx);
        var pago = new Pago(Guid.NewGuid(), Guid.NewGuid(), 1500m, "pref-1");

        await repo.AddAsync(pago);
        await repo.SaveChangesAsync();

        var encontrado = await repo.GetByExternalReferenceAsync(pago.Id);
        Assert.NotNull(encontrado);
        Assert.Equal(pago.Id, encontrado!.Id);
    }

    [Fact]
    public async Task GetByCuotaIdAsync_DevuelveLosPagosDeLaCuota()
    {
        using var ctx = CrearContexto();
        var repo = new PagoRepository(ctx);
        var cuotaId = Guid.NewGuid();
        var otraCuotaId = Guid.NewGuid();

        await repo.AddAsync(new Pago(cuotaId, Guid.NewGuid(), 1500m, "pref-1"));
        await repo.AddAsync(new Pago(cuotaId, Guid.NewGuid(), 1500m, "pref-2"));
        await repo.AddAsync(new Pago(otraCuotaId, Guid.NewGuid(), 1500m, "pref-3"));
        await repo.SaveChangesAsync();

        var pagos = (await repo.GetByCuotaIdAsync(cuotaId)).ToList();
        Assert.Equal(2, pagos.Count);
        Assert.All(pagos, p => Assert.Equal(cuotaId, p.CuotaId));
    }

    [Fact]
    public async Task GetBySocioIdAsync_DevuelveHistorialOrdenadoDescConCuota()
    {
        using var ctx = CrearContexto();
        var repo = new PagoRepository(ctx);
        var socioId = Guid.NewGuid();

        var cuota = CrearCuota(socioId);
        ctx.Cuotas.Add(cuota);
        await ctx.SaveChangesAsync();

        var pagoViejo = new Pago(cuota.Id, socioId, 1500m, "pref-viejo");
        var pagoNuevo = new Pago(cuota.Id, socioId, 1500m, "pref-nuevo");
        // Forzar orden temporal: persistir viejo, esperar, persistir nuevo.
        await repo.AddAsync(pagoViejo);
        await repo.SaveChangesAsync();
        await Task.Delay(5);
        await repo.AddAsync(pagoNuevo);
        await repo.SaveChangesAsync();

        // Pago de otro socio no debe aparecer.
        await repo.AddAsync(new Pago(Guid.NewGuid(), Guid.NewGuid(), 1500m, "pref-otro"));
        await repo.SaveChangesAsync();

        var historial = (await repo.GetBySocioIdAsync(socioId)).ToList();

        Assert.Equal(2, historial.Count);
        Assert.All(historial, p => Assert.Equal(socioId, p.SocioId));
        // Ordenado por fecha de creación descendente (el más nuevo primero).
        Assert.True(historial[0].FechaCreacion >= historial[1].FechaCreacion);
        // Include Cuota.
        Assert.NotNull(historial[0].Cuota);
        Assert.Equal("Plan Test", historial[0].Cuota.NombrePlan);
    }
}
