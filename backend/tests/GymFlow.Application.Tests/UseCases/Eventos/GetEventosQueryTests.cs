using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Eventos;

public class GetEventosQueryTests
{
    private static Evento CrearEvento(Unidad unidad)
    {
        var evento = new Evento("Torneo de verano", "Torneo abierto", DateTime.UtcNow.AddDays(10), unidad.Id);
        typeof(Evento).GetProperty("Unidad")!.SetValue(evento, unidad);
        return evento;
    }

    [Fact]
    public async Task ExecuteAsync_FiltraPorUnidad_MapeaDto()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo");
        var evento = CrearEvento(unidad);

        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetAllAsync(unidad.Id, false, null))
            .ReturnsAsync(new[] { evento });

        var sut = new GetEventosQuery(repo.Object);
        var result = (await sut.ExecuteAsync(unidad.Id, false)).ToList();

        Assert.Single(result);
        Assert.Equal(evento.Id, result[0].Id);
        Assert.Equal("Torneo de verano", result[0].Titulo);
        Assert.Equal("Gimnasio Nuevo Malvín", result[0].UnidadNombre);
        Assert.Equal(unidad.Id, result[0].UnidadId);
        Assert.True(result[0].EstaActivo);
        repo.Verify(r => r.GetAllAsync(unidad.Id, false, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PropagaUnidadesPermitidasAlRepo()
    {
        // Filtro del rol Dueño: la query reenvía las unidades visibles al repo.
        var unidadesPermitidas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetAllAsync(null, false, unidadesPermitidas))
            .ReturnsAsync(Array.Empty<Evento>());

        var sut = new GetEventosQuery(repo.Object);
        await sut.ExecuteAsync(unidadId: null, incluirInactivos: false, unidadesPermitidas: unidadesPermitidas);

        repo.Verify(r => r.GetAllAsync(null, false, unidadesPermitidas), Times.Once);
    }
}
