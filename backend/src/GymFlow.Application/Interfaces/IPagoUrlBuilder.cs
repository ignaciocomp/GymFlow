namespace GymFlow.Application.Interfaces;

/// <summary>
/// Construye las URLs que Mercado Pago necesita para una preferencia de Checkout Pro:
/// las back_urls (a las que MP redirige al socio según el resultado — apuntan al FRONTEND)
/// y la notification_url (a la que MP envía el webhook — apunta a la API pública).
/// La implementación (Infrastructure) lee las bases desde configuración.
/// </summary>
public interface IPagoUrlBuilder
{
    /// <summary>back_urls (success/failure/pending) hacia el portal del socio.</summary>
    BackUrls BuildBackUrls();

    /// <summary>URL pública del webhook (POST /api/pagos/webhook) para las notificaciones de MP.</summary>
    string BuildNotificationUrl();
}
