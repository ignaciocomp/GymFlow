using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class GenerarCuotasCommandTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<ICuotaGeneradorService> _generador = new();

    private GenerarCuotasCommand CrearCommand() =>
        new(_socioRepo.Object, _cuotaRepo.Object, _generador.Object);

    private static Socio CrearSocioConPlan(out UsuarioUnidad uu, Guid? planId = null)
    {
        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Test", apellido: "Socio",
            correo: "socio@test.com", passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);

        uu = new UsuarioUnidad(socio.Id, Guid.NewGuid(), planId ?? Guid.NewGuid());
        socio.UnidadesAsignadas.Add(uu);
        return socio;
    }

    [Fact]
    public async Task ExecuteAsync_SinSocios_RetornaCeroYNoGenera()
    {
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new List<Socio>());

        var resultado = await CrearCommand().ExecuteAsync();

        Assert.Equal(0, resultado.Generadas);
        _generador.Verify(g => g.GenerarCuotaAsync(It.IsAny<Guid>(), It.IsAny<UsuarioUnidad>(), It.IsAny<DateTime>()), Times.Never);
        _cuotaRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SocioSinCuotaPrevia_GeneraCuota()
    {
        var socio = CrearSocioConPlan(out var uu);
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socio });
        _cuotaRepo.Setup(r => r.GetUltimaCuotaAsync(socio.Id, uu.UnidadId)).ReturnsAsync((Cuota?)null);

        var resultado = await CrearCommand().ExecuteAsync();

        Assert.Equal(1, resultado.Generadas);
        _generador.Verify(g => g.GenerarCuotaAsync(socio.Id, uu, It.IsAny<DateTime>()), Times.Once);
        _cuotaRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UltimaCuotaVencida_GeneraCuota()
    {
        var socio = CrearSocioConPlan(out var uu);
        var vencida = new Cuota(socio.Id, uu.UnidadId, Guid.NewGuid(), "Plan", 2500m, DateTime.UtcNow.AddMonths(-2));
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socio });
        _cuotaRepo.Setup(r => r.GetUltimaCuotaAsync(socio.Id, uu.UnidadId)).ReturnsAsync(vencida);

        var resultado = await CrearCommand().ExecuteAsync();

        Assert.Equal(1, resultado.Generadas);
        _generador.Verify(g => g.GenerarCuotaAsync(socio.Id, uu, vencida.FechaVencimiento), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UltimaCuotaVigente_NoGenera()
    {
        var socio = CrearSocioConPlan(out var uu);
        var vigente = new Cuota(socio.Id, uu.UnidadId, Guid.NewGuid(), "Plan", 2500m, DateTime.UtcNow);
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socio });
        _cuotaRepo.Setup(r => r.GetUltimaCuotaAsync(socio.Id, uu.UnidadId)).ReturnsAsync(vigente);

        var resultado = await CrearCommand().ExecuteAsync();

        Assert.Equal(0, resultado.Generadas);
        _generador.Verify(g => g.GenerarCuotaAsync(It.IsAny<Guid>(), It.IsAny<UsuarioUnidad>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UnidadSinPlan_NoGenera()
    {
        var socio = new Socio(
            rolSocioId: Guid.NewGuid(), nombre: "Test", apellido: "Socio",
            correo: "socio@test.com", passwordHash: "hash", fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true, tipoDocumento: TipoDocumento.CI, telefono: null,
            documentoIdentidad: "12345672", fechaNacimiento: null);
        socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, Guid.NewGuid())); // sin plan
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socio });

        var resultado = await CrearCommand().ExecuteAsync();

        Assert.Equal(0, resultado.Generadas);
        _generador.Verify(g => g.GenerarCuotaAsync(It.IsAny<Guid>(), It.IsAny<UsuarioUnidad>(), It.IsAny<DateTime>()), Times.Never);
    }
}
