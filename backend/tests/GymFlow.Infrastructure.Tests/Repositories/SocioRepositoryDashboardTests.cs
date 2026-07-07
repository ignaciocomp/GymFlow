using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Repositories;

/// <summary>
/// Total de socios activos para el dashboard (RF-18): count DISTINCT (un socio asignado a
/// dos unidades cuenta una sola vez), con filtro opcional por unidades.
/// </summary>
public class SocioRepositoryDashboardTests
{
    private static GymFlowDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GymFlowDbContext(options);
    }

    private static Socio CrearSocio(bool activo = true)
    {
        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Maria",
            apellido: "Lopez",
            correo: $"{Guid.NewGuid():N}@test.com",
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.Pasaporte);
        if (!activo) socio.Desactivar();
        return socio;
    }

    [Fact]
    public async Task CountActivos_SinFiltro_CuentaDistinctYExcluyeInactivos()
    {
        using var ctx = CrearContexto();
        var unidad1 = new Unidad("Espacio Mora", "Dir 1");
        var unidad2 = new Unidad("Espacio Sayago", "Dir 2");

        var enAmbas = CrearSocio();
        var soloEn1 = CrearSocio();
        var inactivo = CrearSocio(activo: false);

        ctx.AddRange(unidad1, unidad2, enAmbas, soloEn1, inactivo);
        ctx.UsuarioUnidades.AddRange(
            new UsuarioUnidad(enAmbas.Id, unidad1.Id),
            new UsuarioUnidad(enAmbas.Id, unidad2.Id),
            new UsuarioUnidad(soloEn1.Id, unidad1.Id),
            new UsuarioUnidad(inactivo.Id, unidad1.Id));
        await ctx.SaveChangesAsync();

        var repo = new SocioRepository(ctx);

        // enAmbas cuenta UNA vez aunque esté en dos unidades; el inactivo no cuenta.
        Assert.Equal(2, await repo.CountActivosAsync());
    }

    [Fact]
    public async Task CountActivos_ConUnidades_CuentaSoloSociosDeEsasUnidades()
    {
        using var ctx = CrearContexto();
        var unidad1 = new Unidad("Espacio Mora", "Dir 1");
        var unidad2 = new Unidad("Espacio Sayago", "Dir 2");

        var en1 = CrearSocio();
        var en2 = CrearSocio();

        ctx.AddRange(unidad1, unidad2, en1, en2);
        ctx.UsuarioUnidades.AddRange(
            new UsuarioUnidad(en1.Id, unidad1.Id),
            new UsuarioUnidad(en2.Id, unidad2.Id));
        await ctx.SaveChangesAsync();

        var repo = new SocioRepository(ctx);

        Assert.Equal(1, await repo.CountActivosAsync(new[] { unidad1.Id }));
        Assert.Equal(0, await repo.CountActivosAsync(Array.Empty<Guid>()));
    }
}
