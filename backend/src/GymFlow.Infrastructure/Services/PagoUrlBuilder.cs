using GymFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Construye las URLs de Mercado Pago desde configuración:
/// - back_urls (redirección del socio) desde <c>MercadoPago:BackUrlBase</c> (base del FRONTEND).
/// - notification_url (webhook) desde <c>MercadoPago:ApiBaseUrl</c> (base PÚBLICA de la API).
///   Si <c>ApiBaseUrl</c> no está seteado, cae a <c>BackUrlBase</c> (útil en dev/simulado).
/// </summary>
public class PagoUrlBuilder : IPagoUrlBuilder
{
    private readonly IConfiguration _configuration;

    public PagoUrlBuilder(IConfiguration configuration) => _configuration = configuration;

    public BackUrls BuildBackUrls()
    {
        var frontBase = (_configuration["MercadoPago:BackUrlBase"] ?? "http://localhost:5173").TrimEnd('/');
        return new BackUrls(
            Success: $"{frontBase}/portal/pago/resultado?status=approved",
            Failure: $"{frontBase}/portal/pago/resultado?status=failure",
            Pending: $"{frontBase}/portal/pago/resultado?status=pending");
    }

    public string BuildNotificationUrl()
    {
        // La notification_url debe ser públicamente accesible por MP (la API), no el frontend.
        var apiBase = _configuration["MercadoPago:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBase))
            apiBase = _configuration["MercadoPago:BackUrlBase"] ?? "http://localhost:5173";
        return $"{apiBase.TrimEnd('/')}/api/pagos/webhook";
    }
}
