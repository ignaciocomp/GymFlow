using System.Reflection;
using System.Security.Claims;
using GymFlow.API.Controllers;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Pagos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GymFlow.Application.Tests.Controllers;

/// <summary>
/// Cubre los tres endpoints de RF-21 (iniciar pago, webhook de MP, historial),
/// incluyendo la semántica de seguridad del webhook (401 solo ante firma inválida,
/// 200 en el resto) y las anotaciones de autorización.
/// </summary>
public class PagosControllerTests
{
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<IPagoRepository> _pagoRepo = new();
    private readonly Mock<IMercadoPagoService> _mpService = new();
    private readonly Mock<IPagoUrlBuilder> _urlBuilder = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<ILogger<PagosController>> _logger = new();

    private readonly Guid _socioId = Guid.NewGuid();

    public PagosControllerTests()
    {
        _urlBuilder.Setup(u => u.BuildBackUrls())
            .Returns(new BackUrls("https://f/s", "https://f/f", "https://f/p"));
        _urlBuilder.Setup(u => u.BuildNotificationUrl()).Returns("https://api/api/pagos/webhook");
    }

    private PagosController CrearController()
    {
        var iniciar = new IniciarPagoCuotaCommand(
            _cuotaRepo.Object, _pagoRepo.Object, _mpService.Object, _urlBuilder.Object);
        var webhook = new ProcesarWebhookPagoCommand(
            _mpService.Object, _pagoRepo.Object, _cuotaRepo.Object, _socioRepo.Object,
            _emailService.Object, _auditLogger.Object);
        var misPagos = new GetMisPagosQuery(_pagoRepo.Object);

        var controller = new PagosController(iniciar, webhook, misPagos, _logger.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _socioId.ToString()),
        }, "test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user },
        };
        return controller;
    }

    private static Cuota CrearCuota(Guid socioId) =>
        new(socioId, Guid.NewGuid(), Guid.NewGuid(), "Plan Full", 2500m, DateTime.UtcNow);

    // --- POST /api/pagos/iniciar ---

    [Fact]
    public async Task Iniciar_CuotaPendiente_RetornaOkConInitPoint()
    {
        var cuota = CrearCuota(_socioId);
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);
        _mpService.Setup(s => s.CrearPreferenciaAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BackUrls>()))
            .ReturnsAsync(new PreferenciaResultado("pref-1", "https://mp/init"));

        var result = await CrearController().Iniciar(new IniciarPagoRequest(cuota.Id));

        var ok = Assert.IsType<OkObjectResult>(result);
        var initPoint = (string)ok.Value!.GetType().GetProperty("initPoint")!.GetValue(ok.Value)!;
        Assert.Equal("https://mp/init", initPoint);
    }

    [Fact]
    public async Task Iniciar_CuotaAjena_RetornaForbid()
    {
        var cuota = CrearCuota(Guid.NewGuid()); // de otro socio
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        var result = await CrearController().Iniciar(new IniciarPagoRequest(cuota.Id));

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Iniciar_CuotaNoExiste_RetornaNotFound()
    {
        _cuotaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cuota?)null);

        var result = await CrearController().Iniciar(new IniciarPagoRequest(Guid.NewGuid()));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Iniciar_CuotaYaPagada_RetornaConflict()
    {
        var cuota = CrearCuota(_socioId);
        cuota.MarcarComoPagada();
        _cuotaRepo.Setup(r => r.GetByIdAsync(cuota.Id)).ReturnsAsync(cuota);

        var result = await CrearController().Iniciar(new IniciarPagoRequest(cuota.Id));

        Assert.IsType<ConflictObjectResult>(result);
    }

    // --- POST /api/pagos/webhook ---

    [Fact]
    public async Task Webhook_FirmaInvalida_Retorna401()
    {
        _mpService.Setup(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(false);

        var controller = CrearController();
        var result = await controller.Webhook(new WebhookRequest { Data = new WebhookData { Id = "999" } });

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, status.StatusCode);
    }

    [Fact]
    public async Task Webhook_Procesado_Retorna200()
    {
        _mpService.Setup(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(true);
        _mpService.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>())).ReturnsAsync((PagoMpInfo?)null); // Ignorado

        var result = await CrearController().Webhook(new WebhookRequest { Data = new WebhookData { Id = "999" } });

        var ok = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public async Task Webhook_LeeDataIdDelQueryStringSiNoVieneEnBody()
    {
        string? dataIdUsado = null;
        _mpService.Setup(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Callback<string?, string?, string>((_, _, id) => dataIdUsado = id)
            .Returns(false);

        var controller = CrearController();
        controller.HttpContext.Request.QueryString = new QueryString("?data.id=from-query");

        await controller.Webhook(body: null);

        Assert.Equal("from-query", dataIdUsado);
    }

    [Fact]
    public async Task Webhook_LeeHeadersDeFirma()
    {
        string? sig = null; string? reqId = null;
        _mpService.Setup(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Callback<string?, string?, string>((s, r, _) => { sig = s; reqId = r; })
            .Returns(false);

        var controller = CrearController();
        controller.HttpContext.Request.Headers["x-signature"] = "ts=1,v1=abc";
        controller.HttpContext.Request.Headers["x-request-id"] = "req-42";

        await controller.Webhook(new WebhookRequest { Data = new WebhookData { Id = "999" } });

        Assert.Equal("ts=1,v1=abc", sig);
        Assert.Equal("req-42", reqId);
    }

    // --- POST /api/pagos/webhook — IPN legacy (?topic=payment&id=...) ---

    [Fact]
    public async Task Webhook_IpnTopicPayment_ProcesaSinValidarFirma_Retorna200()
    {
        _mpService.Setup(m => m.ObtenerPagoAsync(It.IsAny<string>())).ReturnsAsync((PagoMpInfo?)null);

        var controller = CrearController();
        controller.HttpContext.Request.QueryString = new QueryString("?topic=payment&id=123456");

        var result = await controller.Webhook(body: null);

        var ok = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        // IPN no tiene firma validable: NUNCA se llama a ValidarFirma…
        _mpService.Verify(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
        // …pero SÍ se consulta el estado real del pago en MP con el id del query.
        _mpService.Verify(m => m.ObtenerPagoAsync("123456"), Times.Once);
    }

    [Fact]
    public async Task Webhook_IpnTopicMerchantOrder_NoProcesa_Retorna200()
    {
        var controller = CrearController();
        controller.HttpContext.Request.QueryString = new QueryString("?topic=merchant_order&id=555");

        var result = await controller.Webhook(body: null);

        var ok = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        _mpService.Verify(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
        _mpService.Verify(m => m.ObtenerPagoAsync(It.IsAny<string>()), Times.Never);
    }

    // --- POST /api/pagos/webhook — formato moderno ---

    [Fact]
    public async Task Webhook_ModernoConDataIdEnQueryYBody_FirmaConElValorDelQuery()
    {
        // MP firma el data.id del QUERY: si viene en query y body, para la firma manda el query.
        string? dataIdUsado = null;
        _mpService.Setup(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Callback<string?, string?, string>((_, _, id) => dataIdUsado = id)
            .Returns(false);

        var controller = CrearController();
        controller.HttpContext.Request.QueryString = new QueryString("?data.id=from-query&type=payment");

        await controller.Webhook(new WebhookRequest { Data = new WebhookData { Id = "from-body" } });

        Assert.Equal("from-query", dataIdUsado);
    }

    [Fact]
    public async Task Webhook_ModernoConTypeDistintoDePayment_NoProcesa_Retorna200()
    {
        var controller = CrearController();

        var result = await controller.Webhook(
            new WebhookRequest { Type = "subscription", Data = new WebhookData { Id = "999" } });

        var ok = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        _mpService.Verify(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
        _mpService.Verify(m => m.ObtenerPagoAsync(It.IsAny<string>()), Times.Never);
    }

    // --- POST /api/pagos/webhook — sin formato reconocible ---

    [Fact]
    public async Task Webhook_SinNingunFormato_Retorna200SinProcesarYLogueaWarning()
    {
        var controller = CrearController();

        var result = await controller.Webhook(body: null);

        var ok = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        _mpService.Verify(m => m.ValidarFirma(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
        _mpService.Verify(m => m.ObtenerPagoAsync(It.IsAny<string>()), Times.Never);
        // Antes esto era un 200 silencioso; ahora tiene que quedar rastro (Warning).
        _logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((_, _) => true),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // --- GET /api/pagos/mis-pagos ---

    [Fact]
    public async Task MisPagos_RetornaOkConHistorialDelSocio()
    {
        var cuota = CrearCuota(_socioId);
        var pago = new Pago(cuota.Id, _socioId, 2500m, "pref-1");
        typeof(Pago).GetProperty(nameof(Pago.Cuota))!.SetValue(pago, cuota);
        _pagoRepo.Setup(r => r.GetBySocioIdAsync(_socioId)).ReturnsAsync(new[] { pago });

        var result = await CrearController().MisPagos();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var pagos = Assert.IsAssignableFrom<IEnumerable<PagoDto>>(ok.Value);
        Assert.Single(pagos);
        _pagoRepo.Verify(r => r.GetBySocioIdAsync(_socioId), Times.Once);
    }

    // --- Atributos de autorización (reflexión) ---

    [Fact]
    public void Webhook_TieneAllowAnonymous()
    {
        var method = typeof(PagosController).GetMethod(nameof(PagosController.Webhook))!;
        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Controller_TieneAuthorize()
    {
        Assert.NotNull(typeof(PagosController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Iniciar_NoTieneAllowAnonymous()
    {
        var method = typeof(PagosController).GetMethod(nameof(PagosController.Iniciar))!;
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void MisPagos_NoTieneAllowAnonymous()
    {
        var method = typeof(PagosController).GetMethod(nameof(PagosController.MisPagos))!;
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Webhook_EsHttpPostEnRutaEsperada()
    {
        var method = typeof(PagosController).GetMethod(nameof(PagosController.Webhook))!;
        var attr = method.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("webhook", attr!.Template);
    }

    [Fact]
    public void Iniciar_EsHttpPostEnRutaEsperada()
    {
        var method = typeof(PagosController).GetMethod(nameof(PagosController.Iniciar))!;
        var attr = method.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("iniciar", attr!.Template);
    }

    [Fact]
    public void MisPagos_EsHttpGetEnRutaEsperada()
    {
        var method = typeof(PagosController).GetMethod(nameof(PagosController.MisPagos))!;
        var attr = method.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("mis-pagos", attr!.Template);
    }
}
