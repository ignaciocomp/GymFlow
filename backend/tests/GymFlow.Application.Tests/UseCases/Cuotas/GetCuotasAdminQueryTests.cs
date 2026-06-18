using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class GetCuotasAdminQueryTests
{
    [Fact]
    public async Task ExecuteAsync_SocioExiste_RetornaCuotas()
    {
        var socio = CrearSocio("12345672");
        var cuotas = new List<Cuota>
        {
            new(socio.Id, Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow),
        };

        var socioRepo = new Mock<ISocioRepository>();
        socioRepo.Setup(r => r.GetByDocumentoIdentidadAsync("12345672")).ReturnsAsync(socio);

        var cuotaRepo = new Mock<ICuotaRepository>();
        cuotaRepo.Setup(r => r.SearchAsync(socio.Id, null, null, null, null, true)).ReturnsAsync(cuotas);

        var sut = new GetCuotasAdminQuery(cuotaRepo.Object, socioRepo.Object);
        var result = (await sut.ExecuteAsync("12345672", null, null, null, null, true)).ToList();

        Assert.Single(result);
        Assert.Equal("Plan Test", result[0].NombrePlan);
    }

    [Fact]
    public async Task ExecuteAsync_SocioNoExiste_ThrowsKeyNotFoundException()
    {
        var socioRepo = new Mock<ISocioRepository>();
        socioRepo.Setup(r => r.GetByDocumentoIdentidadAsync(It.IsAny<string>())).ReturnsAsync((Socio?)null);
        var cuotaRepo = new Mock<ICuotaRepository>();

        var sut = new GetCuotasAdminQuery(cuotaRepo.Object, socioRepo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ExecuteAsync("99999999", null, null, null, null));
    }

    [Fact]
    public async Task ExecuteAsync_SocioEnUnidadPermitida_RetornaCuotas()
    {
        var unidad = Guid.NewGuid();
        var socio = CrearSocio("12345672");
        socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, unidad));
        var cuotas = new List<Cuota>
        {
            new(socio.Id, Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow),
        };

        var socioRepo = new Mock<ISocioRepository>();
        socioRepo.Setup(r => r.GetByDocumentoIdentidadAsync("12345672")).ReturnsAsync(socio);
        var cuotaRepo = new Mock<ICuotaRepository>();
        cuotaRepo.Setup(r => r.SearchAsync(socio.Id, null, null, null, null, true)).ReturnsAsync(cuotas);

        var sut = new GetCuotasAdminQuery(cuotaRepo.Object, socioRepo.Object);
        var result = (await sut.ExecuteAsync("12345672", null, null, null, null, true,
            unidadesPermitidas: new[] { unidad })).ToList();

        Assert.Single(result);
    }

    [Fact]
    public async Task ExecuteAsync_SocioFueraDeUnidadPermitida_RetornaVacio()
    {
        var socio = CrearSocio("12345672");
        socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, Guid.NewGuid()));

        var socioRepo = new Mock<ISocioRepository>();
        socioRepo.Setup(r => r.GetByDocumentoIdentidadAsync("12345672")).ReturnsAsync(socio);
        var cuotaRepo = new Mock<ICuotaRepository>();

        var sut = new GetCuotasAdminQuery(cuotaRepo.Object, socioRepo.Object);
        var result = await sut.ExecuteAsync("12345672", null, null, null, null, true,
            unidadesPermitidas: new[] { Guid.NewGuid() });

        Assert.Empty(result);
        cuotaRepo.Verify(r => r.SearchAsync(
            It.IsAny<Guid>(), It.IsAny<EstadoCuota?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteBySocioIdAsync_SocioFueraDeUnidadPermitida_RetornaVacio()
    {
        var socio = CrearSocio("12345672");
        socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, Guid.NewGuid()));

        var socioRepo = new Mock<ISocioRepository>();
        socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        var cuotaRepo = new Mock<ICuotaRepository>();

        var sut = new GetCuotasAdminQuery(cuotaRepo.Object, socioRepo.Object);
        var result = await sut.ExecuteBySocioIdAsync(socio.Id, null, null, null, null, true,
            unidadesPermitidas: new[] { Guid.NewGuid() });

        Assert.Empty(result);
        cuotaRepo.Verify(r => r.SearchAsync(
            It.IsAny<Guid>(), It.IsAny<EstadoCuota?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteBySocioIdAsync_SocioEnUnidadPermitida_RetornaCuotas()
    {
        var unidad = Guid.NewGuid();
        var socio = CrearSocio("12345672");
        socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, unidad));
        var cuotas = new List<Cuota>
        {
            new(socio.Id, Guid.NewGuid(), Guid.NewGuid(), "Plan Test", 2500m, DateTime.UtcNow),
        };

        var socioRepo = new Mock<ISocioRepository>();
        socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        var cuotaRepo = new Mock<ICuotaRepository>();
        cuotaRepo.Setup(r => r.SearchAsync(socio.Id, null, null, null, null, true)).ReturnsAsync(cuotas);

        var sut = new GetCuotasAdminQuery(cuotaRepo.Object, socioRepo.Object);
        var result = (await sut.ExecuteBySocioIdAsync(socio.Id, null, null, null, null, true,
            unidadesPermitidas: new[] { unidad })).ToList();

        Assert.Single(result);
    }

    private static Socio CrearSocio(string documento)
    {
        return new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Test",
            apellido: "Socio",
            correo: "test@test.com",
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: "099000000",
            documentoIdentidad: documento,
            fechaNacimiento: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
