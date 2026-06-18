using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Empleados;
using GymFlow.Domain.Entities;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Empleados;

public class GetEmpleadosQueryTests
{
    private static (Mock<IEmpleadoRepository>, Mock<IRolRepository>) Mocks()
    {
        var emp = new Mock<IEmpleadoRepository>();
        var rol = new Mock<IRolRepository>();
        rol.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Rol>());
        return (emp, rol);
    }

    [Fact]
    public async Task ExecuteAsync_SinUnidadesPermitidas_PasaNullAlRepo()
    {
        var (emp, rol) = Mocks();
        emp.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Empleado>());
        var sut = new GetEmpleadosQuery(emp.Object, rol.Object);

        await sut.ExecuteAsync();

        emp.Verify(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConUnidadesPermitidas_LasPropagaAlRepo()
    {
        var permitidas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var (emp, rol) = Mocks();
        emp.Setup(r => r.GetAllAsync(
                It.IsAny<bool?>(),
                It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Empleado>());
        var sut = new GetEmpleadosQuery(emp.Object, rol.Object);

        await sut.ExecuteAsync(unidadesPermitidas: permitidas);

        emp.Verify(r => r.GetAllAsync(
            null,
            It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
