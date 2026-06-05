using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Inscripciones;
using GymFlow.Domain.Entities;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Inscripciones;

public class GetMisInscripcionesQueryTests
{
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();

    private GetMisInscripcionesQuery CrearQuery() => new(_inscripcionRepo.Object);

    private static InscripcionClase CrearInscripcion(Guid socioId)
    {
        var claseId = Guid.NewGuid();
        var insc = new InscripcionClase(claseId, socioId);
        var claseFake = new Clase("Yoga", "desc", 10, 60, "Laura", Guid.NewGuid());
        typeof(InscripcionClase).GetProperty("Clase")!.SetValue(insc, claseFake);
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
        _inscripcionRepo.Setup(r => r.GetConteoActivasPorClasesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(inscripciones.ToDictionary(i => i.ClaseId, _ => 2));

        var result = (await CrearQuery().ExecuteAsync(socioId)).ToList();

        Assert.Equal(3, result.Count);
        _inscripcionRepo.Verify(r => r.GetConteoActivasPorClasesAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        _inscripcionRepo.Verify(r => r.GetInscripcionesActivasCountAsync(It.IsAny<Guid>()), Times.Never);
    }
}
