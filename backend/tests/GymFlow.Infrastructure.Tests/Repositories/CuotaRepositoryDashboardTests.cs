using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Repositories;

/// <summary>
/// Counts de cuotas para el dashboard (RF-18): próximas a vencer (ventana [hoy, hoy+5]),
/// vencidas (vencimiento &lt; hoy) y pagadas del mes (FechaPago en el mes actual).
/// Calculados en vivo (RN-17), con filtro opcional por unidades.
/// </summary>
public class CuotaRepositoryDashboardTests
{
    private static readonly DateTime Hoy = DateTime.UtcNow.Date;

    private static GymFlowDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GymFlowDbContext(options);
    }

    /// <summary>Crea una cuota Pendiente que vence en la fecha indicada (el ctor fija vencimiento = emisión + 1 mes).</summary>
    private static Cuota CrearCuotaQueVence(DateTime vencimiento, Guid? unidadId = null) =>
        new(Guid.NewGuid(), unidadId ?? Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 1500m, vencimiento.AddMonths(-1));

    private static Cuota CrearCuotaPagada(DateTime fechaPago, Guid? unidadId = null)
    {
        var cuota = CrearCuotaQueVence(Hoy, unidadId);
        cuota.MarcarComoPagada();
        typeof(Cuota).GetProperty(nameof(Cuota.FechaPago))!.SetValue(cuota, fechaPago);
        return cuota;
    }

    [Fact]
    public async Task CountPendientesPorVencer_CuentaSoloPendientesDentroDeLaVentana()
    {
        using var ctx = CrearContexto();
        ctx.Cuotas.AddRange(
            CrearCuotaQueVence(Hoy),               // borde inferior: cuenta
            CrearCuotaQueVence(Hoy.AddDays(5)),    // borde superior: cuenta
            CrearCuotaQueVence(Hoy.AddDays(6)),    // fuera de la ventana: no
            CrearCuotaQueVence(Hoy.AddDays(-1)),   // ya vencida: no
            CrearCuotaPagada(Hoy));                // pagada: no
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);
        var count = await repo.CountPendientesPorVencerAsync(Hoy, Hoy.AddDays(5));

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountPendientesVencidas_CuentaSoloPendientesConVencimientoAnteriorAHoy()
    {
        using var ctx = CrearContexto();
        var vencidaPagada = CrearCuotaPagada(Hoy);
        typeof(Cuota).GetProperty(nameof(Cuota.FechaVencimiento))!.SetValue(vencidaPagada, Hoy.AddDays(-3));

        ctx.Cuotas.AddRange(
            CrearCuotaQueVence(Hoy.AddDays(-1)),   // vencida: cuenta
            CrearCuotaQueVence(Hoy.AddDays(-30)),  // vencida: cuenta
            CrearCuotaQueVence(Hoy),               // vence hoy, todavía no vencida: no
            vencidaPagada);                        // pagada: no
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);
        var count = await repo.CountPendientesVencidasAsync(Hoy);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountPagadasDelMes_CuentaSoloPagadasConFechaPagoEnElMes()
    {
        using var ctx = CrearContexto();
        ctx.Cuotas.AddRange(
            CrearCuotaPagada(Hoy),                  // este mes: cuenta
            CrearCuotaPagada(Hoy.AddMonths(-1)),    // mes anterior: no
            CrearCuotaQueVence(Hoy));               // pendiente: no
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);
        var count = await repo.CountPagadasDelMesAsync(Hoy.Year, Hoy.Month);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Counts_ConUnidades_FiltranPorUnidad()
    {
        using var ctx = CrearContexto();
        var unidad1 = Guid.NewGuid();
        var unidad2 = Guid.NewGuid();
        ctx.Cuotas.AddRange(
            CrearCuotaQueVence(Hoy.AddDays(2), unidad1),
            CrearCuotaQueVence(Hoy.AddDays(2), unidad2),
            CrearCuotaQueVence(Hoy.AddDays(-1), unidad1),
            CrearCuotaQueVence(Hoy.AddDays(-1), unidad2),
            CrearCuotaPagada(Hoy, unidad1),
            CrearCuotaPagada(Hoy, unidad2));
        await ctx.SaveChangesAsync();

        var repo = new CuotaRepository(ctx);
        var soloUnidad1 = new[] { unidad1 };

        Assert.Equal(1, await repo.CountPendientesPorVencerAsync(Hoy, Hoy.AddDays(5), soloUnidad1));
        Assert.Equal(1, await repo.CountPendientesVencidasAsync(Hoy, soloUnidad1));
        Assert.Equal(1, await repo.CountPagadasDelMesAsync(Hoy.Year, Hoy.Month, soloUnidad1));

        // null = sin restricción: cuenta ambas unidades.
        Assert.Equal(2, await repo.CountPendientesPorVencerAsync(Hoy, Hoy.AddDays(5)));
    }
}
