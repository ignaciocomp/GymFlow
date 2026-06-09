using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Clases;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Clases;

public class GetClasesQueryTests
{
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();

    private GetClasesQuery CrearQuery() =>
        new(_claseRepo.Object, _horarioRepo.Object, _inscripcionRepo.Object);

    private static Clase CrearClase(string nombre) =>
        new(nombre, "desc", 10, 60, "Laura", Guid.NewGuid());

    private static HorarioClase CrearHorario(Guid claseId) =>
        new(claseId, DiaSemana.Martes, new TimeOnly(10, 0), new TimeOnly(11, 0), "Sala A");

    [Fact]
    public async Task NoHaceNMas1_UsaBatchDeHorariosUnaSolaVez()
    {
        var clase1 = CrearClase("Yoga");
        var clase2 = CrearClase("Pilates");
        var clase3 = CrearClase("Funcional");
        _claseRepo.Setup(r => r.GetAllAsync(false))
            .ReturnsAsync(new[] { clase1, clase2, clase3 });

        var horario1A = CrearHorario(clase1.Id);
        var horario1B = CrearHorario(clase1.Id);
        var horario2A = CrearHorario(clase2.Id);
        _horarioRepo.Setup(r => r.GetByClaseIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, List<HorarioClase>>
            {
                [clase1.Id] = [horario1A, horario1B],
                [clase2.Id] = [horario2A],
            });

        _inscripcionRepo.Setup(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>
            {
                [horario1A.Id] = 2,
                [horario1B.Id] = 3,
                [horario2A.Id] = 4,
            });

        var result = (await CrearQuery().ExecuteAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(5, result.Single(c => c.Id == clase1.Id).InscripcionesActivas);
        Assert.Equal(4, result.Single(c => c.Id == clase2.Id).InscripcionesActivas);
        Assert.Equal(0, result.Single(c => c.Id == clase3.Id).InscripcionesActivas);

        _horarioRepo.Verify(r => r.GetByClaseIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        _horarioRepo.Verify(r => r.GetByClaseIdAsync(It.IsAny<Guid>()), Times.Never);
        _inscripcionRepo.Verify(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
    }

    [Fact]
    public async Task SinClases_NoConsultaHorariosNiConteos()
    {
        _claseRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(Array.Empty<Clase>());

        var result = (await CrearQuery().ExecuteAsync()).ToList();

        Assert.Empty(result);
        _horarioRepo.Verify(r => r.GetByClaseIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
        _inscripcionRepo.Verify(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
    }
}
