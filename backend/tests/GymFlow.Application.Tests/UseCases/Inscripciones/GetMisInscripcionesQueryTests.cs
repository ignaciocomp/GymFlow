using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Inscripciones;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Inscripciones;

public class GetMisInscripcionesQueryTests
{
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();

    private GetMisInscripcionesQuery CrearQuery() => new(_inscripcionRepo.Object);

    private static InscripcionClase CrearInscripcion(Guid socioId)
    {
        var claseFake = new Clase("Yoga", "desc", 10, 60, "Laura", Guid.NewGuid());
        var horario = new HorarioClase(claseFake.Id, DiaSemana.Martes, new TimeOnly(10, 0), new TimeOnly(11, 0), "Sala A");
        typeof(HorarioClase).GetProperty("Clase")!.SetValue(horario, claseFake);
        var insc = new InscripcionClase(horario.Id, socioId);
        typeof(InscripcionClase).GetProperty("HorarioClase")!.SetValue(insc, horario);
        return insc;
    }

    [Fact]
    public async Task NoHaceNMas1_UsaConteoBatchUnaSolaVez()
    {
        var socioId = Guid.NewGuid();
        var inscripciones = new[]
        {
            CrearInscripcion(socioId),
            CrearInscripcion(socioId),
            CrearInscripcion(socioId),
        };

        _inscripcionRepo.Setup(r => r.GetBySocioIdAsync(socioId)).ReturnsAsync(inscripciones);
        _inscripcionRepo.Setup(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(inscripciones.ToDictionary(i => i.HorarioClaseId, _ => 2));

        var result = (await CrearQuery().ExecuteAsync(socioId)).ToList();

        Assert.Equal(3, result.Count);
        _inscripcionRepo.Verify(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        _inscripcionRepo.Verify(r => r.GetInscripcionesActivasCountAsync(It.IsAny<Guid>()), Times.Never);
    }
}
