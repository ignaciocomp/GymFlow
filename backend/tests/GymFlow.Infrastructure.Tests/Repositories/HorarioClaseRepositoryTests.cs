using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Tests.Repositories;

/// <summary>
/// E2E-19: GetAllAsync (portal y grilla admin) no debe listar horarios de clases
/// canceladas; al reactivar la clase, sus horarios vuelven a listarse.
/// Mismo criterio que ya aplicaba GetByDiaAsync.
/// </summary>
public class HorarioClaseRepositoryTests
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
    public async Task GetAll_ExcluyeHorariosDeClasesCanceladas()
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
        var horarios = (await repo.GetAllAsync()).ToList();

        Assert.Single(horarios);
        Assert.Equal("Funcional", horarios[0].Clase.Nombre);
    }

    [Fact]
    public async Task GetAll_AlReactivarLaClase_SusHorariosVuelvenAListarse()
    {
        using var ctx = CrearContexto();
        var unidad = new Unidad("Espacio Mora", "Dir 1");
        var clase = CrearClase(unidad.Id, "Spinning");
        clase.Cancelar();

        ctx.AddRange(unidad, clase, CrearHorario(clase.Id, DiaSemana.Lunes, 9));
        await ctx.SaveChangesAsync();

        var repo = new HorarioClaseRepository(ctx);
        Assert.Empty(await repo.GetAllAsync());

        clase.Reactivar();
        await ctx.SaveChangesAsync();

        Assert.Single(await repo.GetAllAsync());
    }

    [Fact]
    public async Task GetAll_ConUnidad_FiltraYExcluyeCanceladas()
    {
        using var ctx = CrearContexto();
        var unidad1 = new Unidad("Espacio Mora", "Dir 1");
        var unidad2 = new Unidad("Espacio Sayago", "Dir 2");
        var activa1 = CrearClase(unidad1.Id, "Funcional");
        var cancelada1 = CrearClase(unidad1.Id, "Spinning");
        cancelada1.Cancelar();
        var activa2 = CrearClase(unidad2.Id, "Yoga");

        ctx.AddRange(unidad1, unidad2, activa1, cancelada1, activa2,
            CrearHorario(activa1.Id, DiaSemana.Lunes, 8),
            CrearHorario(cancelada1.Id, DiaSemana.Lunes, 9),
            CrearHorario(activa2.Id, DiaSemana.Lunes, 10));
        await ctx.SaveChangesAsync();

        var repo = new HorarioClaseRepository(ctx);
        var horarios = (await repo.GetAllAsync(unidad1.Id)).ToList();

        Assert.Single(horarios);
        Assert.Equal("Funcional", horarios[0].Clase.Nombre);
    }
}
