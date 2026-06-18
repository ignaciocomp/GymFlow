using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Clases;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Clases;

public class GetClasesQueryTests
{
    private static (Mock<IClaseRepository>, Mock<IHorarioClaseRepository>, Mock<IInscripcionClaseRepository>) Mocks()
    {
        var clase = new Mock<IClaseRepository>();
        var horario = new Mock<IHorarioClaseRepository>();
        var inscripcion = new Mock<IInscripcionClaseRepository>();
        horario.Setup(r => r.GetByClaseIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<HorarioClase>());
        inscripcion.Setup(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        return (clase, horario, inscripcion);
    }

    [Fact]
    public async Task ExecuteAsync_SinUnidadId_SinUnidadesPermitidas_PasaNullAlRepo()
    {
        var (clase, horario, inscripcion) = Mocks();
        clase.Setup(r => r.GetAllAsync(false, null)).ReturnsAsync(new List<Clase>());
        var sut = new GetClasesQuery(clase.Object, horario.Object, inscripcion.Object);

        await sut.ExecuteAsync();

        clase.Verify(r => r.GetAllAsync(false, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SinUnidadId_ConUnidadesPermitidas_LasPropagaAGetAll()
    {
        var permitidas = new[] { Guid.NewGuid() };
        var (clase, horario, inscripcion) = Mocks();
        clase.Setup(r => r.GetAllAsync(
                It.IsAny<bool>(),
                It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))))
            .ReturnsAsync(new List<Clase>());
        var sut = new GetClasesQuery(clase.Object, horario.Object, inscripcion.Object);

        await sut.ExecuteAsync(unidadesPermitidas: permitidas);

        clase.Verify(r => r.GetAllAsync(
            false, It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConUnidadId_ConUnidadesPermitidas_LasPropagaAGetByUnidadId()
    {
        var unidad = Guid.NewGuid();
        var permitidas = new[] { unidad };
        var (clase, horario, inscripcion) = Mocks();
        clase.Setup(r => r.GetByUnidadIdAsync(
                unidad, It.IsAny<bool>(),
                It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))))
            .ReturnsAsync(new List<Clase>());
        var sut = new GetClasesQuery(clase.Object, horario.Object, inscripcion.Object);

        await sut.ExecuteAsync(unidad, unidadesPermitidas: permitidas);

        clase.Verify(r => r.GetByUnidadIdAsync(
            unidad, false, It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))), Times.Once);
    }
}
