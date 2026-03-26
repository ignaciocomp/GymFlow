using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Unidades;
using GymFlow.Domain.Entities;
using Moq;

namespace GymFlow.Application.Tests.UseCases;

public class GetUnidadesQueryTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsAllUnidades()
    {
        var mockRepo = new Mock<IUnidadRepository>();
        var unidades = new List<Unidad>
        {
            new("Gimnasio Nuevo Malvín", "Malvín, Montevideo"),
            new("Espacio Mora", "Malvín, Montevideo")
        };
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(unidades);

        var query = new GetUnidadesQuery(mockRepo.Object);
        var result = (await query.ExecuteAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Gimnasio Nuevo Malvín", result[0].Nombre);
        Assert.Equal("Espacio Mora", result[1].Nombre);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmpty_ReturnsEmptyList()
    {
        var mockRepo = new Mock<IUnidadRepository>();
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Unidad>());

        var query = new GetUnidadesQuery(mockRepo.Object);
        var result = (await query.ExecuteAsync()).ToList();

        Assert.Empty(result);
    }
}
