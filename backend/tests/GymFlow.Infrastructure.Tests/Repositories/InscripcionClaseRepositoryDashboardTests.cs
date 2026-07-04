using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Repositories;

/// <summary>
/// Inscripciones para el dashboard (RF-18): últimas N inscripciones activas (con socio,
/// clase y unidad) y conteo de inscripciones activas por día para la gráfica de 7 días.
/// </summary>
public class InscripcionClaseRepositoryDashboardTests
{
    private static GymFlowDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GymFlowDbContext(options);
    }

    private static Socio CrearSocio(string nombre = "Maria") =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: nombre,
            apellido: "Lopez",
            correo: $"{Guid.NewGuid():N}@test.com",
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.Pasaporte);

    private static InscripcionClase CrearInscripcion(Guid horarioId, Guid socioId, DateTime fecha, bool activa = true)
    {
        var inscripcion = new InscripcionClase(horarioId, socioId);
        typeof(InscripcionClase).GetProperty(nameof(InscripcionClase.FechaInscripcion))!.SetValue(inscripcion, fecha);
        if (!activa) inscripcion.Cancelar();
        return inscripcion;
    }

    private static (Unidad Unidad, Clase Clase, HorarioClase Horario) CrearEstructura(string unidadNombre = "Espacio Mora")
    {
        var unidad = new Unidad(unidadNombre, "Dir 1");
        var clase = new Clase("Funcional", "desc", 20, 60, "Ana", unidad.Id);
        var horario = new HorarioClase(clase.Id, DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(9, 0), null);
        return (unidad, clase, horario);
    }

    [Fact]
    public async Task GetRecientes_DevuelveLasUltimasActivasOrdenadasDescConSocioClaseYUnidad()
    {
        using var ctx = CrearContexto();
        var (unidad, clase, horario) = CrearEstructura();
        var socio = CrearSocio();
        var hoy = DateTime.UtcNow;

        ctx.AddRange(unidad, clase, horario, socio,
            CrearInscripcion(horario.Id, socio.Id, hoy.AddDays(-2)),
            CrearInscripcion(horario.Id, socio.Id, hoy.AddDays(-1)),
            CrearInscripcion(horario.Id, socio.Id, hoy),
            CrearInscripcion(horario.Id, socio.Id, hoy, activa: false)); // cancelada: no aparece
        await ctx.SaveChangesAsync();

        var repo = new InscripcionClaseRepository(ctx);
        var recientes = (await repo.GetRecientesAsync(2)).ToList();

        Assert.Equal(2, recientes.Count);
        Assert.True(recientes[0].FechaInscripcion >= recientes[1].FechaInscripcion);
        Assert.All(recientes, i => Assert.True(i.EstaActiva));
        Assert.Equal("Maria", recientes[0].Socio.Nombre);
        Assert.Equal("Funcional", recientes[0].HorarioClase.Clase.Nombre);
        Assert.Equal("Espacio Mora", recientes[0].HorarioClase.Clase.Unidad.Nombre);
    }

    [Fact]
    public async Task GetRecientes_ConUnidades_FiltraPorUnidadDeLaClase()
    {
        using var ctx = CrearContexto();
        var (unidad1, clase1, horario1) = CrearEstructura("Espacio Mora");
        var (unidad2, clase2, horario2) = CrearEstructura("Espacio Sayago");
        var socio = CrearSocio();
        var hoy = DateTime.UtcNow;

        ctx.AddRange(unidad1, clase1, horario1, unidad2, clase2, horario2, socio,
            CrearInscripcion(horario1.Id, socio.Id, hoy),
            CrearInscripcion(horario2.Id, socio.Id, hoy));
        await ctx.SaveChangesAsync();

        var repo = new InscripcionClaseRepository(ctx);
        var recientes = (await repo.GetRecientesAsync(10, new[] { unidad2.Id })).ToList();

        Assert.Single(recientes);
        Assert.Equal(unidad2.Id, recientes[0].HorarioClase.Clase.UnidadId);
    }

    [Fact]
    public async Task GetConteoActivasPorDia_AgrupaPorFechaDentroDelRango()
    {
        using var ctx = CrearContexto();
        var (unidad, clase, horario) = CrearEstructura();
        var socio = CrearSocio();
        var hoy = DateTime.UtcNow.Date;

        ctx.AddRange(unidad, clase, horario, socio,
            CrearInscripcion(horario.Id, socio.Id, hoy.AddHours(9)),
            CrearInscripcion(horario.Id, socio.Id, hoy.AddHours(15)),
            CrearInscripcion(horario.Id, socio.Id, hoy.AddDays(-2).AddHours(10)),
            CrearInscripcion(horario.Id, socio.Id, hoy.AddDays(-10)),                  // fuera del rango: no
            CrearInscripcion(horario.Id, socio.Id, hoy.AddHours(9), activa: false));   // cancelada: no
        await ctx.SaveChangesAsync();

        var repo = new InscripcionClaseRepository(ctx);
        var conteo = await repo.GetConteoActivasPorDiaAsync(hoy.AddDays(-6), hoy);

        Assert.Equal(2, conteo.Count);
        Assert.Equal(2, conteo[hoy]);
        Assert.Equal(1, conteo[hoy.AddDays(-2)]);
    }

    [Fact]
    public async Task GetConteoActivasPorDia_ConUnidades_FiltraPorUnidadDeLaClase()
    {
        using var ctx = CrearContexto();
        var (unidad1, clase1, horario1) = CrearEstructura("Espacio Mora");
        var (unidad2, clase2, horario2) = CrearEstructura("Espacio Sayago");
        var socio = CrearSocio();
        var hoy = DateTime.UtcNow.Date;

        ctx.AddRange(unidad1, clase1, horario1, unidad2, clase2, horario2, socio,
            CrearInscripcion(horario1.Id, socio.Id, hoy.AddHours(9)),
            CrearInscripcion(horario2.Id, socio.Id, hoy.AddHours(10)));
        await ctx.SaveChangesAsync();

        var repo = new InscripcionClaseRepository(ctx);
        var conteo = await repo.GetConteoActivasPorDiaAsync(hoy.AddDays(-6), hoy, new[] { unidad1.Id });

        Assert.Single(conteo);
        Assert.Equal(1, conteo[hoy]);
    }
}
