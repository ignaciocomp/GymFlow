using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Repositories;

/// <summary>
/// E2E-07 (RN-09): TieneCuotaVencidaAsync devuelve true solo si el socio tiene alguna
/// cuota Pendiente con vencimiento anterior a hoy en la unidad indicada.
/// </summary>
public class CuotaRepositoryTests
{
    private static readonly DateTime Hoy = DateTime.UtcNow.Date;

    private static GymFlowDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GymFlowDbContext(options);
    }

    /// <summary>Cuota Pendiente que vence en la fecha indicada (el ctor fija vencimiento = emisión + 1 mes).</summary>
    private static Cuota CrearCuotaQueVence(Guid socioId, Guid unidadId, DateTime vencimiento) =>
        new(socioId, unidadId, Guid.NewGuid(), "Plan Test", 1500m, vencimiento.AddMonths(-1));

    [Fact]
    public async Task ConCuotaPendienteVencidaEnLaUnidad_DevuelveTrue()
    {
        using var ctx = CrearContexto();
        var socioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        ctx.Cuotas.Add(CrearCuotaQueVence(socioId, unidadId, Hoy.AddDays(-1)));
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);

        Assert.True(await repo.TieneCuotaVencidaAsync(socioId, unidadId, Hoy));
    }

    [Fact]
    public async Task CuotaQueVenceHoy_TodaviaNoEstaVencida_DevuelveFalse()
    {
        using var ctx = CrearContexto();
        var socioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        ctx.Cuotas.Add(CrearCuotaQueVence(socioId, unidadId, Hoy));
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);

        Assert.False(await repo.TieneCuotaVencidaAsync(socioId, unidadId, Hoy));
    }

    [Fact]
    public async Task ConCuotaPendienteFutura_DevuelveFalse()
    {
        using var ctx = CrearContexto();
        var socioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        ctx.Cuotas.Add(CrearCuotaQueVence(socioId, unidadId, Hoy.AddDays(10)));
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);

        Assert.False(await repo.TieneCuotaVencidaAsync(socioId, unidadId, Hoy));
    }

    [Fact]
    public async Task CuotaVencidaPagada_DevuelveFalse()
    {
        using var ctx = CrearContexto();
        var socioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var cuota = CrearCuotaQueVence(socioId, unidadId, Hoy.AddDays(-5));
        cuota.MarcarComoPagada();
        ctx.Cuotas.Add(cuota);
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);

        Assert.False(await repo.TieneCuotaVencidaAsync(socioId, unidadId, Hoy));
    }

    [Fact]
    public async Task CuotaVencidaAnulada_DevuelveFalse()
    {
        using var ctx = CrearContexto();
        var socioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var cuota = CrearCuotaQueVence(socioId, unidadId, Hoy.AddDays(-5));
        cuota.Anular();
        ctx.Cuotas.Add(cuota);
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);

        Assert.False(await repo.TieneCuotaVencidaAsync(socioId, unidadId, Hoy));
    }

    [Fact]
    public async Task CuotaVencidaEnOtraUnidad_DevuelveFalse()
    {
        using var ctx = CrearContexto();
        var socioId = Guid.NewGuid();
        var unidadConsulta = Guid.NewGuid();
        var otraUnidad = Guid.NewGuid();
        ctx.Cuotas.Add(CrearCuotaQueVence(socioId, otraUnidad, Hoy.AddDays(-5)));
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);

        Assert.False(await repo.TieneCuotaVencidaAsync(socioId, unidadConsulta, Hoy));
    }

    [Fact]
    public async Task CuotaVencidaDeOtroSocio_DevuelveFalse()
    {
        using var ctx = CrearContexto();
        var unidadId = Guid.NewGuid();
        ctx.Cuotas.Add(CrearCuotaQueVence(Guid.NewGuid(), unidadId, Hoy.AddDays(-5)));
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);

        Assert.False(await repo.TieneCuotaVencidaAsync(Guid.NewGuid(), unidadId, Hoy));
    }
}
