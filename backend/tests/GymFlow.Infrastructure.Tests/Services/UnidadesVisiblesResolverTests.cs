using GymFlow.Application.Interfaces;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Services;
using Moq;
using Xunit;

namespace GymFlow.Infrastructure.Tests.Services;

public class UnidadesVisiblesResolverTests
{
    private static Empleado CrearDueno(Guid userId, params Guid[] unidadIds)
    {
        var empleado = new Empleado("Maurice", "Dueño", "maurice@gymflow.com", "h", RolesSeed.DuenoRolId);
        foreach (var unidadId in unidadIds)
            empleado.UnidadesAsignadas.Add(new UsuarioUnidad(empleado.Id, unidadId));
        return empleado;
    }

    [Fact]
    public async Task Admin_DevuelveNull()
    {
        var empleadoRepo = new Mock<IEmpleadoRepository>();
        var sut = new UnidadesVisiblesResolver(empleadoRepo.Object);

        var result = await sut.ResolverAsync(Guid.NewGuid(), RolesSeed.AdminRolId);

        Assert.Null(result);
        empleadoRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dueno_DevuelveSusUnidades()
    {
        var userId = Guid.NewGuid();
        var unidadA = Guid.NewGuid();
        var unidadB = Guid.NewGuid();
        var empleado = CrearDueno(userId, unidadA, unidadB);

        var empleadoRepo = new Mock<IEmpleadoRepository>();
        empleadoRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(empleado);
        var sut = new UnidadesVisiblesResolver(empleadoRepo.Object);

        var result = await sut.ResolverAsync(userId, RolesSeed.DuenoRolId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Contains(unidadA, result);
        Assert.Contains(unidadB, result);
    }

    [Fact]
    public async Task Dueno_SinEmpleado_DevuelveVacio()
    {
        var userId = Guid.NewGuid();
        var empleadoRepo = new Mock<IEmpleadoRepository>();
        empleadoRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((Empleado?)null);
        var sut = new UnidadesVisiblesResolver(empleadoRepo.Object);

        var result = await sut.ResolverAsync(userId, RolesSeed.DuenoRolId);

        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public async Task OtroEmpleado_DevuelveNull()
    {
        var empleadoRepo = new Mock<IEmpleadoRepository>();
        var sut = new UnidadesVisiblesResolver(empleadoRepo.Object);

        var result = await sut.ResolverAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
        empleadoRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
