using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Auth;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Auth;

public class LoginConGoogleCommandTests
{
    private const string IdToken = "id-token-de-google";
    private const string Sub = "google-sub-123";
    private const string Correo = "socio@test.com";

    private readonly Mock<IGoogleTokenValidator> _tokenValidator = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();

    private LoginConGoogleCommand CrearCommand() => new(_tokenValidator.Object, _socioRepo.Object);

    private static Socio SocioFake() =>
        new(RolesSeed.SocioRolId, "María", "López", Correo, null,
            DateTime.UtcNow, true, TipoDocumento.CI, null, "12345672", null);

    private void SetupTokenValido(bool emailVerificado = true) =>
        _tokenValidator.Setup(v => v.ValidarAsync(IdToken))
            .ReturnsAsync(new GoogleTokenPayload(Sub, Correo, emailVerificado));

    [Fact]
    public async Task ExecuteAsync_SocioActivoConTokenValido_DevuelveSocio()
    {
        SetupTokenValido();
        var socio = SocioFake();
        _socioRepo.Setup(r => r.GetByCorreoAsync(Correo)).ReturnsAsync(socio);

        var resultado = await CrearCommand().ExecuteAsync(IdToken);

        Assert.Same(socio, resultado);
    }

    [Fact]
    public async Task ExecuteAsync_SocioInexistente_LanzaUnauthorizedConMensajeE3()
    {
        SetupTokenValido();
        _socioRepo.Setup(r => r.GetByCorreoAsync(Correo)).ReturnsAsync((Socio?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearCommand().ExecuteAsync(IdToken));

        Assert.Equal("No encontramos una cuenta asociada a este correo.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SocioInactivo_LanzaUnauthorizedConMensajeE3()
    {
        SetupTokenValido();
        var socio = SocioFake();
        socio.DarDeBaja(null);
        _socioRepo.Setup(r => r.GetByCorreoAsync(Correo)).ReturnsAsync(socio);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearCommand().ExecuteAsync(IdToken));

        Assert.Equal("No encontramos una cuenta asociada a este correo.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TokenInvalido_LanzaUnauthorized()
    {
        _tokenValidator.Setup(v => v.ValidarAsync(IdToken))
            .ReturnsAsync((GoogleTokenPayload?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearCommand().ExecuteAsync(IdToken));

        Assert.Equal("Token de Google inválido.", ex.Message);
        _socioRepo.Verify(r => r.GetByCorreoAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EmailNoVerificado_LanzaUnauthorized()
    {
        SetupTokenValido(emailVerificado: false);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearCommand().ExecuteAsync(IdToken));

        Assert.Equal("Token de Google inválido.", ex.Message);
        _socioRepo.Verify(r => r.GetByCorreoAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PrimerLogin_VinculaGoogleUserIdYPersiste()
    {
        SetupTokenValido();
        var socio = SocioFake();
        _socioRepo.Setup(r => r.GetByCorreoAsync(Correo)).ReturnsAsync(socio);

        await CrearCommand().ExecuteAsync(IdToken);

        Assert.Equal(Sub, socio.GoogleUserId);
        _socioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SocioYaVinculado_NoReVinculaNiPersiste()
    {
        SetupTokenValido();
        var socio = SocioFake();
        socio.VincularGoogle("sub-anterior");
        _socioRepo.Setup(r => r.GetByCorreoAsync(Correo)).ReturnsAsync(socio);

        await CrearCommand().ExecuteAsync(IdToken);

        Assert.Equal("sub-anterior", socio.GoogleUserId);
        _socioRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
