using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Portal;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Portal;

public class SolicitarBajaCommandTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private SolicitarBajaCommand CrearCommand() => new(_socioRepo.Object, _auditLogger.Object);

    private static Socio SocioFake() =>
        new(RolesSeed.SocioRolId, "María", "López", "socio@test.com", null,
            DateTime.UtcNow, true, TipoDocumento.CI, null, "12345672", null);

    [Fact]
    public async Task ExecuteAsync_ConMotivo_RegistraAuditoriaConMotivo()
    {
        var socio = SocioFake();
        _socioRepo.Setup(r => r.GetByCorreoAsync("socio@test.com")).ReturnsAsync(socio);

        await CrearCommand().ExecuteAsync("socio@test.com", "Me mudo", Guid.NewGuid(), "María López");

        _auditLogger.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), "María López",
            TipoAccionAuditoria.SolicitudBaja, "Socio", socio.Id,
            It.Is<string>(s => s.Contains("Me mudo"))),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SinMotivo_RegistraAuditoriaSinMotivo()
    {
        var socio = SocioFake();
        _socioRepo.Setup(r => r.GetByCorreoAsync("socio@test.com")).ReturnsAsync(socio);

        await CrearCommand().ExecuteAsync("socio@test.com", null, Guid.NewGuid(), "María López");

        _auditLogger.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), "María López",
            TipoAccionAuditoria.SolicitudBaja, "Socio", socio.Id,
            It.Is<string>(s => s.Contains("solicitó la baja") && !s.Contains("Motivo"))),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SocioNoExiste_LanzaKeyNotFoundException()
    {
        _socioRepo.Setup(r => r.GetByCorreoAsync("noexiste@test.com")).ReturnsAsync((Socio?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync("noexiste@test.com", null, Guid.NewGuid(), "Nadie"));
    }
}
