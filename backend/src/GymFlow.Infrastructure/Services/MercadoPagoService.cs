using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GymFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Integración con Mercado Pago (Checkout Pro) vía HttpClient — sin SDK.
/// Crea preferencias de pago, consulta el estado real de un pago y valida la firma HMAC de los webhooks.
/// Credenciales (AccessToken / WebhookSecret) desde configuración (secrets en prod).
/// </summary>
public class MercadoPagoService : IMercadoPagoService
{
    private const string PreferencesUrl = "https://api.mercadopago.com/checkout/preferences";
    private const string PaymentsUrl = "https://api.mercadopago.com/v1/payments";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(HttpClient httpClient, IConfiguration configuration, ILogger<MercadoPagoService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public bool ValidarFirma(string? xSignature, string? xRequestId, string dataId)
    {
        var secret = _configuration["MercadoPago:WebhookSecret"];
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Validación de firma de webhook MP omitida: WebhookSecret no configurado.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(xSignature) || string.IsNullOrWhiteSpace(dataId))
            return false;

        // El header x-signature tiene el formato "ts=<timestamp>,v1=<hex>".
        string? ts = null;
        string? v1 = null;
        foreach (var parte in xSignature.Split(','))
        {
            var idx = parte.IndexOf('=');
            if (idx <= 0) continue;
            var clave = parte[..idx].Trim();
            var valor = parte[(idx + 1)..].Trim();
            if (clave == "ts") ts = valor;
            else if (clave == "v1") v1 = valor;
        }

        if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(v1))
            return false;

        // Manifest exacto que MP firma: "id:{dataId};request-id:{xRequestId};ts:{ts};".
        // Docs MP: el data.id se firma en minúsculas ("If data.id is returned with uppercase
        // alphanumeric characters, convert it to lowercase") y los valores ausentes se OMITEN
        // del manifest ("If any of the values are not present, remove them from the manifest").
        var idNormalizado = dataId.ToLowerInvariant();
        var requestIdPar = string.IsNullOrWhiteSpace(xRequestId) ? string.Empty : $"request-id:{xRequestId};";
        var manifest = $"id:{idNormalizado};{requestIdPar}ts:{ts};";

        string calculado;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
            calculado = Convert.ToHexString(hash).ToLowerInvariant();
        }

        // Comparación case-insensitive y de tiempo constante (evita timing attacks).
        var recibida = v1.ToLowerInvariant();
        var calculadoBytes = Encoding.ASCII.GetBytes(calculado);
        var recibidaBytes = Encoding.ASCII.GetBytes(recibida);
        return CryptographicOperations.FixedTimeEquals(calculadoBytes, recibidaBytes);
    }

    public async Task<PreferenciaResultado> CrearPreferenciaAsync(Guid pagoId, decimal monto, string descripcion, string notificationUrl, BackUrls backUrls)
    {
        var accessToken = _configuration["MercadoPago:AccessToken"]
            ?? throw new InvalidOperationException("MercadoPago:AccessToken no configurado.");

        var body = new
        {
            items = new[]
            {
                new
                {
                    title = descripcion,
                    quantity = 1,
                    unit_price = monto,
                    currency_id = "UYU"
                }
            },
            external_reference = pagoId.ToString(),
            back_urls = new
            {
                success = backUrls.Success,
                failure = backUrls.Failure,
                pending = backUrls.Pending
            },
            auto_return = "approved",
            notification_url = notificationUrl
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, PreferencesUrl)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var preferenceId = root.GetProperty("id").GetString() ?? string.Empty;
        var initPoint = root.TryGetProperty("init_point", out var ip) ? ip.GetString() ?? string.Empty : string.Empty;

        return new PreferenciaResultado(preferenceId, initPoint);
    }

    public async Task<PagoMpInfo?> ObtenerPagoAsync(string mpPaymentId)
    {
        var accessToken = _configuration["MercadoPago:AccessToken"]
            ?? throw new InvalidOperationException("MercadoPago:AccessToken no configurado.");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{PaymentsUrl}/{mpPaymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("No se pudo consultar el pago {PaymentId} en MP: {Status}.", mpPaymentId, response.StatusCode);
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var estado = root.TryGetProperty("status", out var s) ? s.GetString() ?? string.Empty : string.Empty;
        var medioPago = root.TryGetProperty("payment_method_id", out var m) ? m.GetString() : null;
        var externalReference = root.TryGetProperty("external_reference", out var er) ? er.GetString() : null;
        var paymentId = root.TryGetProperty("id", out var pid)
            ? (pid.ValueKind == JsonValueKind.Number ? pid.GetInt64().ToString() : pid.GetString() ?? mpPaymentId)
            : mpPaymentId;

        return new PagoMpInfo(estado, medioPago, externalReference, paymentId);
    }
}
