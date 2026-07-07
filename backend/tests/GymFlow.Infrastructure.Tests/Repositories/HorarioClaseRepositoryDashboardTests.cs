using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Repositories;

/// <summary>
/// Clases del día para el dashboard (RF-18): horarios de un día de la semana, solo de
/// clases activas, con Clase y Unidad cargadas, filtrables por unidades y ordenados por hora.
/// </summary>
public class HorarioClaseRepositoryDashboardTests
{
    private static GymFlowDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GymFlowDbContext(options);
    }

    private static Clase CrearClase(Guid unidadId, string nombre = "Funcional") =>
        new(nombre, "desc", capacidadMaxima: 20, duracionMinutos: 60, instructor: "Ana", unidadId: unidadId);

    private static HorarioClase CrearHorario(Guid claseId, DiaSemana dia, int hora) =>
        new(claseId, dia, new TimeOnly(hora, 0), new TimeOnly(hora + 1, 0), sala: null);

    [Fact]
    public async Task GetByDia_DevuelveSoloElDiaPedido_OrdenadoPorHoraConClaseYUnidad()
    {
        using var ctx = CrearContexto();
        var unidad = new Unidad("Espacio Mora", "Dir 1");
        var clase = CrearClase(unidad.Id);

        ctx.AddRange(unidad, clase,
            CrearHorario(clase.Id, DiaSemana.Lunes, 18),
            CrearHorario(clase.Id, DiaSemana.Lunes, 8),
            CrearHorario(clase.Id, DiaSemana.Martes, 10));
        await ctx.SaveChangesAsync();

        var repo = new HorarioClaseRepository(ctx);
        var horarios = (await repo.GetByDiaAsync(DiaSemana.Lunes)).ToList();

        Assert.Equal(2, horarios.Count);
        Assert.Equal(new TimeOnly(8, 0), horarios[0].HoraInicio);
        Assert.Equal(new TimeOnly(18, 0), horarios[1].HoraInicio);
        Assert.All(horarios, h => Assert.Equal(DiaSemana.Lunes, h.DiaSemana));
        Assert.Equal("Espacio Mora", horarios[0].Clase.Unidad.Nombre);
    }

    [Fact]
    public async Task GetByDia_ExcluyeClasesCanceladas()
    {
        using var ctx = CrearContexto();
        var unidad = new Unidad("Espacio Mora", "Dir 1");
        var activa = CrearClase(unidad.Id, "Funcional");
        var cancelada = CrearClase(unidad.Id, "Spinning");
        cancelada.Cancelar();

        ctx.AddRange(unidad, activa, cancelada,
            CrearHorario(activa.Id, DiaSemana.Lunes, 8),
            CrearHorario(cancelada.Id, DiaSemana.Lunes, 9));
        await ctx.SaveChangesAsync();

        var repo = new HorarioClaseRepository(ctx);
        var horarios = (await repo.GetByDiaAsync(DiaSemana.Lunes)).ToList();

        Assert.Single(horarios);
        Assert.Equal("Funcional", horarios[0].Clase.Nombre);
    }

    [Fact]
    public async Task GetByDia_ConUnidades_FiltraPorUnidadDeLaClase()
    {
        using var ctx = CrearContexto();
        var unidad1 = new Unidad("Espacio Mora", "Dir 1");
        var unidad2 = new Unidad("Espacio Sayago", "Dir 2");
        var clase1 = CrearClase(unidad1.Id);
        var clase2 = CrearClase(unidad2.Id);

        ctx.AddRange(unidad1, unidad2, clase1, clase2,
            CrearHorario(clase1.Id, DiaSemana.Lunes, 8),
            CrearHorario(clase2.Id, DiaSemana.Lunes, 9));
        await ctx.SaveChangesAsync();

        var repo = new HorarioClaseRepository(ctx);
        var horarios = (await repo.GetByDiaAsync(DiaSemana.Lunes, new[] { unidad1.Id })).ToList();

        Assert.Single(horarios);
        Assert.Equal(unidad1.Id, horarios[0].Clase.UnidadId);
    }
}
