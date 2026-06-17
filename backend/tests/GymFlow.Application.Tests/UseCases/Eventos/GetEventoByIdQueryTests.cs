using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Eventos;

public class GetEventoByIdQueryTests
{
    [Fact]
    public async Task ExecuteAsync_Existe_MapeaDto()
    {
        var unidad = new Unidad("Espacio Mora", "Malvín, Montevideo");
        var evento = new Evento("Charla de nutrición", "Charla abierta", DateTime.UtcNow.AddDays(5), unidad.Id);
        typeof(Evento).GetProperty("Unidad")!.SetValue(evento, unidad);

        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);

        var sut = new GetEventoByIdQuery(repo.Object);
        var result = await sut.ExecuteAsync(evento.Id);

        Assert.Equal(evento.Id, result.Id);
        Assert.Equal("Charla de nutrición", result.Titulo);
        Assert.Equal("Espacio Mora", result.UnidadNombre);
    }

    [Fact]
    public async Task ExecuteAsync_NoExiste_LanzaKeyNotFound()
    {
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Evento?)null);

        var sut = new GetEventoByIdQuery(repo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ExecuteAsync(Guid.NewGuid()));
    }
}
