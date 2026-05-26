using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Portal;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Portal;

public class GetSocioPerfilQueryTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();

    private GetSocioPerfilQuery CrearQuery() => new(_socioRepo.Object);

    private static Socio SocioFake() =>
        new(RolesSeed.SocioRolId, "María", "López", "socio@test.com", null,
            DateTime.UtcNow, true, TipoDocumento.CI, "099123456", "12345672", null);

    [Fact]
    public async Task ExecuteAsync_SocioExiste_RetornaPerfil()
    {
        var socio = SocioFake();
        _socioRepo.Setup(r => r.GetByCorreoAsync("socio@test.com")).ReturnsAsync(socio);

        var result = await CrearQuery().ExecuteAsync("socio@test.com");

        Assert.Equal("María", result.Nombre);
        Assert.Equal("López", result.Apellido);
        Assert.Equal("socio@test.com", result.Correo);
    }

    [Fact]
    public async Task ExecuteAsync_SocioNoExiste_LanzaKeyNotFoundException()
    {
        _socioRepo.Setup(r => r.GetByCorreoAsync("noexiste@test.com")).ReturnsAsync((Socio?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearQuery().ExecuteAsync("noexiste@test.com"));
    }
}
