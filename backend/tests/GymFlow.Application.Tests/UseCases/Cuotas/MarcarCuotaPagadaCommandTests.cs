using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class MarcarCuotaPagadaCommandTests
{
    [Fact]
    public async Task ExecuteAsync_CuotaExiste_MarcaComoPagadaYAudita()
    {
        var cuota = new Cuota(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow);
        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        var audit = new Mock<IAuditLogger>();

        var sut = new MarcarCuotaPagadaCommand(repo.Object, audit.Object);
        await sut.ExecuteAsync(cuota.Id, Guid.NewGuid(), "Admin Test");

        Assert.Equal(EstadoCuota.Pagada, cuota.Estado);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        audit.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id, It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaNoExiste_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<ICuotaRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cuota?)null);
        var audit = new Mock<IAuditLogger>();

        var sut = new MarcarCuotaPagadaCommand(repo.Object, audit.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));
    }
}
