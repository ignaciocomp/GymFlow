using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Portal;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Portal;

public class SolicitarModificacionCommandTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private SolicitarModificacionCommand CrearCommand() => new(_socioRepo.Object, _auditLogger.Object);

    private static Socio SocioFake() =>
        new(RolesSeed.SocioRolId, "María", "López", "socio@test.com", null,
            DateTime.UtcNow, true, TipoDocumento.CI, null, "12345672", null);

    [Fact]
    public async Task ExecuteAsync_DetalleValido_RegistraAuditoria()
    {
        var socio = SocioFake();
        _socioRepo.Setup(r => r.GetByCorreoAsync("socio@test.com")).ReturnsAsync(socio);

        await CrearCommand().ExecuteAsync("socio@test.com", "Cambiar teléfono", Guid.NewGuid(), "María López");

        _auditLogger.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), "María López",
            TipoAccionAuditoria.SolicitudModificacion, "Socio", socio.Id,
            It.Is<string>(s => s.Contains("Cambiar teléfono"))),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DetalleVacio_LanzaArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync("socio@test.com", "  ", Guid.NewGuid(), "María López"));
    }

    [Fact]
    public async Task ExecuteAsync_SocioNoExiste_LanzaKeyNotFoundException()
    {
        _socioRepo.Setup(r => r.GetByCorreoAsync("noexiste@test.com")).ReturnsAsync((Socio?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync("noexiste@test.com", "Cambiar algo", Guid.NewGuid(), "Nadie"));
    }
}
