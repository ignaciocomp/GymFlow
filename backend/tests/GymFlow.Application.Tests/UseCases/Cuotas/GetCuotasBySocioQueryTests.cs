using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class GetCuotasBySocioQueryTests
{
    [Fact]
    public async Task ExecuteAsync_ConCuotas_RetornaDtos()
    {
        var socioId = Guid.NewGuid();
        var cuotas = new List<Cuota>
        {
            new(socioId, Guid.NewGuid(), Guid.NewGuid(), "Plan A", 2500m, DateTime.UtcNow),
            new(socioId, Guid.NewGuid(), Guid.NewGuid(), "Plan B", 3500m, DateTime.UtcNow.AddMonths(-1)),
        };

        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetBySocioIdAsync(socioId)).ReturnsAsync(cuotas);

        var sut = new GetCuotasBySocioQuery(repo.Object);
        var result = (await sut.ExecuteAsync(socioId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Plan A", result[0].NombrePlan);
        Assert.Equal(2500m, result[0].Monto);
    }

    [Fact]
    public async Task ExecuteAsync_SinCuotas_RetornaListaVacia()
    {
        var socioId = Guid.NewGuid();
        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetBySocioIdAsync(socioId)).ReturnsAsync(new List<Cuota>());

        var sut = new GetCuotasBySocioQuery(repo.Object);
        var result = (await sut.ExecuteAsync(socioId)).ToList();

        Assert.Empty(result);
    }
}
