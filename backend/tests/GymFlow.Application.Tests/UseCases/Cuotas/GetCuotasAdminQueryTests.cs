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
