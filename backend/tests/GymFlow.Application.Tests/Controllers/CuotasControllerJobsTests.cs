using System.Reflection;
using GymFlow.API.Authorization;
using GymFlow.API.Controllers;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GymFlow.Application.Tests.Controllers;

/// <summary>
/// Cubre los dos endpoints manuales que disparan los jobs programados
/// (recordatorios de cuotas y generación de cuotas) — issues #24/#28.
/// </summary>
public class CuotasControllerJobsTests
{
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<IRecordatorioCuotaRepository> _recordatorioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificadorInApp> _notificador = new();

    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<ICuotaGeneradorService> _generador = new();

    private CuotasController CrearController(
        ProcesarRecordatoriosCommand? procesar = null,
        GenerarCuotasCommand? generar = null)
    {
        procesar ??= new ProcesarRecordatoriosCommand(
            _cuotaRepo.Object, _recordatorioRepo.Object, _emailService.Object, _notificador.Object);
        generar ??= new GenerarCuotasCommand(
            _socioRepo.Object, _cuotaRepo.Object, _generador.Object);

        return new CuotasController(
            getCuotasBySocioQuery: null!,
            getCuotasAdminQuery: null!,
            marcarPagadaCommand: null!,
            anularCommand: null!,
            revertirPagoCommand: null!,
            revertirAnulacionCommand: null!,
            notificarCommand: null!,
            getSociosConEstadoCuotaQuery: null!,
            unidadesResolver: null!,
            procesarRecordatoriosCommand: procesar,
            generarCuotasCommand: generar);
    }

    // --- POST /api/cuotas/procesar-recordatorios ---

    [Fact]
    public async Task ProcesarRecordatorios_RetornaOkConElResultadoDelCommand()
    {
        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Domain.Entities.Cuota>());

        var result = await CrearController().ProcesarRecordatorios();

        var ok = Assert.IsType<OkObjectResult>(result);
        var resultado = Assert.IsType<ProcesarRecordatoriosResultado>(ok.Value);
        Assert.Equal(0, resultado.Enviados);
        _cuotaRepo.Verify(r => r.GetCuotasParaRecordatorioAsync(It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public void ProcesarRecordatorios_TienePermisoCuotasModificacion()
    {
        var method = typeof(CuotasController).GetMethod(nameof(CuotasController.ProcesarRecordatorios))!;
        var attr = method.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(attr);
        Assert.Equal(Modulo.Cuotas, GetModulo(attr!));
        Assert.Equal(Operacion.Modificacion, GetOperacion(attr!));
    }

    [Fact]
    public void ProcesarRecordatorios_EsHttpPostEnRutaEsperada()
    {
        var method = typeof(CuotasController).GetMethod(nameof(CuotasController.ProcesarRecordatorios))!;
        var attr = method.GetCustomAttribute<HttpPostAttribute>();

        Assert.NotNull(attr);
        Assert.Equal("procesar-recordatorios", attr!.Template);
    }

    // --- POST /api/cuotas/generar ---

    [Fact]
    public async Task Generar_RetornaOkConElResultadoDelCommand()
    {
        _socioRepo.Setup(r => r.GetAllAsync(false))
            .ReturnsAsync(new List<Domain.Entities.Socio>());

        var result = await CrearController().Generar();

        var ok = Assert.IsType<OkObjectResult>(result);
        var resultado = Assert.IsType<GenerarCuotasResultado>(ok.Value);
        Assert.Equal(0, resultado.Generadas);
        _socioRepo.Verify(r => r.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public void Generar_TienePermisoCuotasModificacion()
    {
        var method = typeof(CuotasController).GetMethod(nameof(CuotasController.Generar))!;
        var attr = method.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(attr);
        Assert.Equal(Modulo.Cuotas, GetModulo(attr!));
        Assert.Equal(Operacion.Modificacion, GetOperacion(attr!));
    }

    [Fact]
    public void Generar_EsHttpPostEnRutaEsperada()
    {
        var method = typeof(CuotasController).GetMethod(nameof(CuotasController.Generar))!;
        var attr = method.GetCustomAttribute<HttpPostAttribute>();

        Assert.NotNull(attr);
        Assert.Equal("generar", attr!.Template);
    }

    // RequierePermisoAttribute guarda módulo/operación en campos privados.
    private static Modulo GetModulo(RequierePermisoAttribute attr) =>
        (Modulo)typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(attr)!;

    private static Operacion GetOperacion(RequierePermisoAttribute attr) =>
        (Operacion)typeof(RequierePermisoAttribute)
            .GetField("_operacion", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(attr)!;
}
