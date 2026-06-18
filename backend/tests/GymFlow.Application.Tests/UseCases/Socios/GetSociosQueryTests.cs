using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Socios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.UseCases.Socios;

public class GetSociosQueryTests
{
    [Fact]
    public async Task ExecuteAsync_SinUnidadesPermitidas_PasaNullAlRepo()
    {
        var repo = new Mock<ISocioRepository>();
        repo.Setup(r => r.SearchAsync(null, null, null, null, null))
            .ReturnsAsync(new List<Socio>());
        var sut = new GetSociosQuery(repo.Object);

        await sut.ExecuteAsync();

        repo.Verify(r => r.SearchAsync(null, null, null, null, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConUnidadesPermitidas_LasPropagaAlRepo()
    {
        var permitidas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var repo = new Mock<ISocioRepository>();
        repo.Setup(r => r.SearchAsync(
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<bool?>(),
                It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))))
            .ReturnsAsync(new List<Socio>());
        var sut = new GetSociosQuery(repo.Object);

        await sut.ExecuteAsync(unidadesPermitidas: permitidas);

        repo.Verify(r => r.SearchAsync(
            null, null, null, null,
            It.Is<IReadOnlyCollection<Guid>>(s => s.SequenceEqual(permitidas))), Times.Once);
    }
}
