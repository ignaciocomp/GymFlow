using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Services;
using Moq;

namespace GymFlow.Infrastructure.Tests.Services;

public class CuotaGeneradorServiceTests
{
    private static Plan CrearPlan()
    {
        return new Plan("Plan Test", 2500m, "Desc", Guid.NewGuid());
    }

    [Fact]
    public async Task GenerarCuotaAsync_ConPlanAsignado_CreaCuota()
    {
        var plan = CrearPlan();
        var socioId = Guid.NewGuid();
        var uu = new UsuarioUnidad(socioId, Guid.NewGuid(), plan.Id);
        var fecha = DateTime.UtcNow;

        var cuotaRepo = new Mock<ICuotaRepository>();
        var planRepo = new Mock<IPlanRepository>();
        planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var sut = new CuotaGeneradorService(cuotaRepo.Object, planRepo.Object);
        var cuota = await sut.GenerarCuotaAsync(socioId, uu, fecha);

        Assert.Equal("Plan Test", cuota.NombrePlan);
        Assert.Equal(2500m, cuota.Monto);
        Assert.Equal(fecha, cuota.FechaEmision);
        Assert.Equal(fecha.AddMonths(1), cuota.FechaVencimiento);
        cuotaRepo.Verify(r => r.AddAsync(It.IsAny<Cuota>()), Times.Once);
    }

    [Fact]
    public async Task GenerarCuotaAsync_SinPlanAsignado_ThrowsInvalidOperationException()
    {
        var socioId = Guid.NewGuid();
        var uu = new UsuarioUnidad(socioId, Guid.NewGuid());

        var cuotaRepo = new Mock<ICuotaRepository>();
        var planRepo = new Mock<IPlanRepository>();

        var sut = new CuotaGeneradorService(cuotaRepo.Object, planRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerarCuotaAsync(socioId, uu, DateTime.UtcNow));
    }

    [Fact]
    public async Task GenerarCuotasRetroactivasAsync_GeneraCuotasFaltantes()
    {
        var plan = CrearPlan();
        var socioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var uu = new UsuarioUnidad(socioId, unidadId, plan.Id);
        var fechaAlta = DateTime.UtcNow.AddMonths(-3);

        var cuotaRepo = new Mock<ICuotaRepository>();
        cuotaRepo.Setup(r => r.SearchAsync(socioId, null, null, null, unidadId, true))
            .ReturnsAsync(new List<Cuota>());

        var planRepo = new Mock<IPlanRepository>();
        planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var sut = new CuotaGeneradorService(cuotaRepo.Object, planRepo.Object);
        var result = await sut.GenerarCuotasRetroactivasAsync(socioId, uu, fechaAlta);

        Assert.True(result.Count >= 3);
        cuotaRepo.Verify(r => r.AddAsync(It.IsAny<Cuota>()), Times.Exactly(result.Count));
    }

    [Fact]
    public async Task GenerarCuotasRetroactivasAsync_NoGeneraDuplicadas()
    {
        var plan = CrearPlan();
        var socioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var uu = new UsuarioUnidad(socioId, unidadId, plan.Id);
        var fechaAlta = DateTime.UtcNow.AddMonths(-2);

        var cuotaExistente = new Cuota(socioId, unidadId, plan.Id, plan.Nombre, plan.Precio, fechaAlta);
        var cuotaRepo = new Mock<ICuotaRepository>();
        cuotaRepo.Setup(r => r.SearchAsync(socioId, null, null, null, unidadId, true))
            .ReturnsAsync(new List<Cuota> { cuotaExistente });

        var planRepo = new Mock<IPlanRepository>();
        planRepo.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var sut = new CuotaGeneradorService(cuotaRepo.Object, planRepo.Object);
        var result = await sut.GenerarCuotasRetroactivasAsync(socioId, uu, fechaAlta);

        Assert.All(result, c =>
            Assert.NotEqual((c.FechaEmision.Year, c.FechaEmision.Month),
                (cuotaExistente.FechaEmision.Year, cuotaExistente.FechaEmision.Month)));
    }
}
