using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class GetSociosConEstadoCuotaQueryTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();

    private GetSociosConEstadoCuotaQuery CrearQuery() => new(_socioRepo.Object, _cuotaRepo.Object);

    private static Socio CrearSocio(string nombre = "Test") =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: nombre, apellido: "Apellido",
            correo: $"{nombre.ToLower()}@test.com",
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);

    private static Cuota CuotaConVencimiento(Guid socioId, Guid unidadId, DateTime vencimiento)
    {
        var emision = vencimiento.AddMonths(-1);
        return new Cuota(socioId, unidadId, Guid.NewGuid(), "Plan", 2500m, emision);
    }

    [Fact]
    public async Task ExecuteAsync_SocioSinCuotasPendientes_EstadoAlDia()
    {
        var socio = CrearSocio();
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socio });
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null)).ReturnsAsync(Array.Empty<Cuota>());

        var result = (await CrearQuery().ExecuteAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(EstadoGeneralCuotas.AlDia, result[0].Estado);
        Assert.Equal(0, result[0].CuotasPendientes);
        Assert.Equal(0, result[0].CuotasVencidas);
    }

    [Fact]
    public async Task ExecuteAsync_SocioConCuotaPendienteFutura_EstadoPendiente()
    {
        var socio = CrearSocio();
        var cuota = CuotaConVencimiento(socio.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(10));
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socio });
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null)).ReturnsAsync(new[] { cuota });

        var result = (await CrearQuery().ExecuteAsync()).ToList();

        Assert.Equal(EstadoGeneralCuotas.Pendiente, result[0].Estado);
        Assert.Equal(1, result[0].CuotasPendientes);
        Assert.Equal(0, result[0].CuotasVencidas);
    }

    [Fact]
    public async Task ExecuteAsync_SocioConCuotaPendienteVencida_EstadoVencido()
    {
        var socio = CrearSocio();
        var cuota = CuotaConVencimiento(socio.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(-5));
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socio });
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null)).ReturnsAsync(new[] { cuota });

        var result = (await CrearQuery().ExecuteAsync()).ToList();

        Assert.Equal(EstadoGeneralCuotas.Vencido, result[0].Estado);
        Assert.Equal(1, result[0].CuotasPendientes);
        Assert.Equal(1, result[0].CuotasVencidas);
    }

    [Fact]
    public async Task ExecuteAsync_MultiplesSocios_NoHaceN1Queries()
    {
        // Verificamos que se llama a GetCuotasPendientesDeTodosLosSociosAsync UNA sola vez,
        // sin importar la cantidad de socios. Anti-regresión del fix del N+1.
        var socios = Enumerable.Range(1, 100).Select(i => CrearSocio($"Socio{i}")).ToList();
        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(socios);
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null)).ReturnsAsync(Array.Empty<Cuota>());

        await CrearQuery().ExecuteAsync();

        _cuotaRepo.Verify(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null), Times.Once);
        // Y NUNCA debe llamar al método de búsqueda por socio (que sería el patrón N+1)
        _cuotaRepo.Verify(r =>
            r.SearchAsync(It.IsAny<Guid>(), It.IsAny<EstadoCuota?>(), It.IsAny<int?>(),
                          It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OrdenaVencidosPrimero()
    {
        var socioAlDia = CrearSocio("AlDia");
        var socioPendiente = CrearSocio("Pendiente");
        var socioVencido = CrearSocio("Vencido");

        _socioRepo.Setup(r => r.GetAllAsync(false))
            .ReturnsAsync(new[] { socioAlDia, socioPendiente, socioVencido });

        var cuotaPendiente = CuotaConVencimiento(socioPendiente.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(10));
        var cuotaVencida = CuotaConVencimiento(socioVencido.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(-5));
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null))
            .ReturnsAsync(new[] { cuotaPendiente, cuotaVencida });

        var result = (await CrearQuery().ExecuteAsync()).ToList();

        Assert.Equal(EstadoGeneralCuotas.Vencido, result[0].Estado);
        Assert.Equal(EstadoGeneralCuotas.Pendiente, result[1].Estado);
        Assert.Equal(EstadoGeneralCuotas.AlDia, result[2].Estado);
    }
}
