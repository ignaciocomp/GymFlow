using System.Reflection;
using System.Security.Claims;
using GymFlow.API.Authorization;
using GymFlow.API.Controllers;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Dashboard;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace GymFlow.Application.Tests.Controllers;

/// <summary>
/// DashboardController (RF-18): snapshot y stream SSE con permiso Dashboard-Lectura,
/// filtrado server-side por IUnidadesVisiblesResolver y 403 (no 500) cuando la unidad
/// pedida no está permitida — en el stream ANTES de iniciar el loop.
/// </summary>
public class DashboardControllerTests
{
    private readonly Mock<IUnidadesVisiblesResolver> _resolver = new();
    private readonly Mock<IUnidadRepository> _unidadRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();

    private readonly Unidad _mora = new("Espacio Mora", "Dir 1");
    private readonly Unidad _sayago = new("Espacio Sayago", "Dir 2");

    public DashboardControllerTests()
    {
        _unidadRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { _mora, _sayago });
        _horarioRepo.Setup(r => r.GetByDiaAsync(It.IsAny<DiaSemana>(), It.IsAny<IReadOnlyCollection<Guid>?>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());
        _inscripcionRepo.Setup(r => r.GetRecientesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<Guid>?>()))
            .ReturnsAsync(Array.Empty<InscripcionClase>());
        _inscripcionRepo.Setup(r => r.GetConteoActivasPorDiaAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>()))
            .ReturnsAsync(new Dictionary<DateTime, int>());
    }

    private DashboardController CrearController(IReadOnlyCollection<Guid>? unidadesPermitidas, CancellationToken requestAborted = default)
    {
        _resolver.Setup(r => r.ResolverAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(unidadesPermitidas);

        var query = new GetDashboardQuery(
            _unidadRepo.Object, _socioRepo.Object, _cuotaRepo.Object, _horarioRepo.Object, _inscripcionRepo.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("rolId", Guid.NewGuid().ToString()),
        }, "test"));

        var httpContext = new DefaultHttpContext { User = user, RequestAborted = requestAborted };
        httpContext.Response.Body = new MemoryStream();

        var controller = new DashboardController(query, _resolver.Object, Options.Create(new MvcJsonOptions()));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    // --- GET /api/dashboard ---

    [Fact]
    public async Task Get_DevuelveElSnapshotDelQuery()
    {
        _socioRepo.Setup(r => r.CountActivosAsync(null)).ReturnsAsync(30);

        var result = await CrearController(unidadesPermitidas: null).Get(unidadId: null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DashboardDto>(ok.Value);
        Assert.Equal(30, dto.SociosActivos.Total);
        Assert.Equal(2, dto.Unidades.Count);
    }

    [Fact]
    public async Task Get_UnidadNoPermitida_RetornaForbid()
    {
        var result = await CrearController(unidadesPermitidas: new[] { _mora.Id }).Get(unidadId: _sayago.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    // --- GET /api/dashboard/stream ---

    [Fact]
    public async Task Stream_UnidadNoPermitida_RetornaForbidAntesDeIniciarElStream()
    {
        var controller = CrearController(unidadesPermitidas: new[] { _mora.Id });

        var result = await controller.Stream(unidadId: _sayago.Id);

        Assert.IsType<ForbidResult>(result);
        // No arrancó el stream: la respuesta no quedó marcada como SSE.
        Assert.NotEqual("text/event-stream", controller.Response.ContentType);
    }

    [Fact]
    public async Task Stream_SeteaHeadersSseSinBuffering()
    {
        // Token ya cancelado: valida permisos, setea headers y sale del loop sin escribir.
        var controller = CrearController(unidadesPermitidas: null, requestAborted: new CancellationToken(canceled: true));

        var result = await controller.Stream(unidadId: null);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal("text/event-stream", controller.Response.ContentType);
        Assert.Equal("no-cache", controller.Response.Headers.CacheControl);
        Assert.Equal("no", controller.Response.Headers["X-Accel-Buffering"]);
    }

    // --- Permisos y rutas (RN-16) ---

    [Theory]
    [InlineData(nameof(DashboardController.Get))]
    [InlineData(nameof(DashboardController.Stream))]
    public void Endpoints_RequierenPermisoDashboardLectura(string metodo)
    {
        var attr = typeof(DashboardController).GetMethod(metodo)!.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(attr);
        Assert.Equal(Modulo.Dashboard, GetModulo(attr!));
        Assert.Equal(Operacion.Lectura, GetOperacion(attr!));
    }

    [Fact]
    public void Stream_EsHttpGetEnRutaStream()
    {
        var attr = typeof(DashboardController).GetMethod(nameof(DashboardController.Stream))!
            .GetCustomAttribute<HttpGetAttribute>();

        Assert.NotNull(attr);
        Assert.Equal("stream", attr!.Template);
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
