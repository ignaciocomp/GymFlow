using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Horarios;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Horarios;

public class GetHorariosQueryTests
{
    private static (Mock<IHorarioClaseRepository>, Mock<IInscripcionClaseRepository>) Mocks()
    {
        var horario = new Mock<IHorarioClaseRepository>();
        var inscripcion = new Mock<IInscripcionClaseRepository>();
        inscripcion.Setup(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        return (horario, inscripcion);
    }

    [Fact]
    public async Task ExecuteAsync_SinUnidadesPermitidas_PasaNullAlRepo()
    {
        var (horario, inscripcion) = Mocks();
        horario.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<HorarioClase>());
        var sut = new GetHorariosQuery(horario.Object, inscripcion.Object);

        await sut.ExecuteAsync();

        horario.Verify(r => r.GetAllAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConUnidadesPermitidas_LasPropagaAlRepo()
    {
        var permitidas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var (horario, inscripcion) = Mocks();
        horario.Setup(r => r.GetAllAsync(
                It.IsAny<Guid?>(),
                It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))))
            .ReturnsAsync(new List<HorarioClase>());
        var sut = new GetHorariosQuery(horario.Object, inscripcion.Object);

        await sut.ExecuteAsync(unidadesPermitidas: permitidas);

        horario.Verify(r => r.GetAllAsync(
            null, It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))), Times.Once);
    }
}
