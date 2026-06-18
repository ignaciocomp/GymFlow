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
    public async Task ExecuteAsync_ConUnidadesPermitidas_SoloDevuelveSociosDeEsasUnidades()
    {
        // El Dueño NO selecciona unidad por query param: pasa unidadId null.
        // Aun así, solo debe ver socios de sus unidades permitidas (no cross-unidad).
        var unidadDelDueno = Guid.NewGuid();
        var unidadAjena = Guid.NewGuid();

        var socioVisible = CrearSocio("Visible");
        socioVisible.UnidadesAsignadas.Add(new UsuarioUnidad(socioVisible.Id, unidadDelDueno));

        var socioAjeno = CrearSocio("Ajeno");
        socioAjeno.UnidadesAsignadas.Add(new UsuarioUnidad(socioAjeno.Id, unidadAjena));

        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socioVisible, socioAjeno });
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null)).ReturnsAsync(Array.Empty<Cuota>());

        var result = (await CrearQuery().ExecuteAsync(unidadId: null, unidadesPermitidas: new[] { unidadDelDueno })).ToList();

        Assert.Single(result);
        Assert.Equal("Visible", result[0].Nombre);
    }

    [Fact]
    public async Task ExecuteAsync_UnidadIdAjenaAUnidadesPermitidas_NoFiltraFueraDeLoPermitido()
    {
        // El Dueño pide por query param una unidad que NO es suya: igual no debe ver nada de esa unidad.
        var unidadDelDueno = Guid.NewGuid();
        var unidadAjena = Guid.NewGuid();

        var socioAjeno = CrearSocio("Ajeno");
        socioAjeno.UnidadesAsignadas.Add(new UsuarioUnidad(socioAjeno.Id, unidadAjena));

        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socioAjeno });
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(It.IsAny<Guid?>())).ReturnsAsync(Array.Empty<Cuota>());

        var result = (await CrearQuery().ExecuteAsync(unidadId: unidadAjena, unidadesPermitidas: new[] { unidadDelDueno })).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_SinUnidadesPermitidas_DevuelveTodos()
    {
        // null = sin restricción (Admin): comportamiento actual intacto.
        var socioA = CrearSocio("A");
        socioA.UnidadesAsignadas.Add(new UsuarioUnidad(socioA.Id, Guid.NewGuid()));
        var socioB = CrearSocio("B");
        socioB.UnidadesAsignadas.Add(new UsuarioUnidad(socioB.Id, Guid.NewGuid()));

        _socioRepo.Setup(r => r.GetAllAsync(false)).ReturnsAsync(new[] { socioA, socioB });
        _cuotaRepo.Setup(r => r.GetCuotasPendientesDeTodosLosSociosAsync(null)).ReturnsAsync(Array.Empty<Cuota>());

        var result = (await CrearQuery().ExecuteAsync(unidadId: null, unidadesPermitidas: null)).ToList();

        Assert.Equal(2, result.Count);
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
