namespace GymFlow.Application.Interfaces;

public interface IMercadoPagoService
{
    /// <summary>
    /// Crea una preferencia de Checkout Pro en Mercado Pago y devuelve el id de la preferencia
    /// y el init_point (URL a la que redirigir al socio).
    /// </summary>
    Task<PreferenciaResultado> CrearPreferenciaAsync(Guid pagoId, decimal monto, string descripcion, string notificationUrl, BackUrls backUrls);

    /// <summary>
    /// Consulta el estado real de un pago en la API de Mercado Pago (fuente de verdad; no se confía en el payload del webhook).
    /// </summary>
    Task<PagoMpInfo?> ObtenerPagoAsync(string mpPaymentId);

    /// <summary>
    /// Valida la firma HMAC del webhook (header x-signature) contra el WebhookSecret configurado.
    /// Devuelve false si falta el secret, el header está mal formado, o la firma no coincide.
    /// </summary>
    bool ValidarFirma(string? xSignature, string? xRequestId, string dataId);
}

public record PreferenciaResultado(string PreferenceId, string InitPoint);

public record PagoMpInfo(string Estado, string? MedioPago, string? ExternalReference, string PaymentId);

public record BackUrls(string Success, string Failure, string Pending);
