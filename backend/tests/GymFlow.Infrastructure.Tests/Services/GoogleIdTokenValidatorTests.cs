using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GymFlow.Infrastructure.Tests.Services;

public class GoogleIdTokenValidatorTests
{
    private static GoogleIdTokenValidator CrearValidator()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Google:ClientId"]).Returns("test-client-id.apps.googleusercontent.com");
        return new GoogleIdTokenValidator(config.Object, NullLogger<GoogleIdTokenValidator>.Instance);
    }

    [Fact]
    public async Task ValidarAsync_TokenMalformado_DevuelveNull()
    {
        var sut = CrearValidator();

        var resultado = await sut.ValidarAsync("esto-no-es-un-jwt");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ValidarAsync_TokenVacio_DevuelveNull()
    {
        var sut = CrearValidator();

        var resultado = await sut.ValidarAsync("");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ValidarAsync_JwtBienFormadoPeroNoFirmadoPorGoogle_DevuelveNull()
    {
        // JWT con estructura válida (header.payload.firma) pero firma trucha:
        // cualquier excepción de validación debe traducirse en null, nunca propagarse.
        var jwtTrucho = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NSIsImVtYWlsIjoidGVzdEB0ZXN0LmNvbSJ9.firma-invalida";
        var sut = CrearValidator();

        var resultado = await sut.ValidarAsync(jwtTrucho);

        Assert.Null(resultado);
    }
}
