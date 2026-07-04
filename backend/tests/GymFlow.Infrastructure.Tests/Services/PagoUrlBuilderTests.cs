using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace GymFlow.Infrastructure.Tests.Services;

public class PagoUrlBuilderTests
{
    private static PagoUrlBuilder CrearBuilder(string? apiBaseUrl = null, string? backUrlBase = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["MercadoPago:ApiBaseUrl"] = apiBaseUrl,
            ["MercadoPago:BackUrlBase"] = backUrlBase
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new PagoUrlBuilder(config);
    }

    // --- BuildNotificationUrl: debe pedir formato webhook moderno (source_news=webhooks) ---

    [Fact]
    public void BuildNotificationUrl_ConApiBaseUrl_AgregaSourceNewsWebhooks()
    {
        var builder = CrearBuilder(apiBaseUrl: "https://api.gymflow.uy");

        Assert.Equal(
            "https://api.gymflow.uy/api/pagos/webhook?source_news=webhooks",
            builder.BuildNotificationUrl());
    }

    [Fact]
    public void BuildNotificationUrl_BaseConBarraFinal_NoDuplicaBarras()
    {
        var builder = CrearBuilder(apiBaseUrl: "https://api.gymflow.uy/");

        Assert.Equal(
            "https://api.gymflow.uy/api/pagos/webhook?source_news=webhooks",
            builder.BuildNotificationUrl());
    }

    [Fact]
    public void BuildNotificationUrl_BaseConPath_ConservaElPath()
    {
        var builder = CrearBuilder(apiBaseUrl: "https://midominio.com/gymflow/");

        Assert.Equal(
            "https://midominio.com/gymflow/api/pagos/webhook?source_news=webhooks",
            builder.BuildNotificationUrl());
    }

    [Fact]
    public void BuildNotificationUrl_SinApiBaseUrl_CaeABackUrlBase()
    {
        var builder = CrearBuilder(apiBaseUrl: null, backUrlBase: "https://front.gymflow.uy");

        Assert.Equal(
            "https://front.gymflow.uy/api/pagos/webhook?source_news=webhooks",
            builder.BuildNotificationUrl());
    }

    // --- BuildBackUrls (sin cambios de comportamiento) ---

    [Fact]
    public void BuildBackUrls_UsaBackUrlBaseDelFrontend()
    {
        var builder = CrearBuilder(backUrlBase: "https://front.gymflow.uy/");

        var backUrls = builder.BuildBackUrls();

        Assert.Equal("https://front.gymflow.uy/portal/pago/resultado?status=approved", backUrls.Success);
        Assert.Equal("https://front.gymflow.uy/portal/pago/resultado?status=failure", backUrls.Failure);
        Assert.Equal("https://front.gymflow.uy/portal/pago/resultado?status=pending", backUrls.Pending);
    }
}
