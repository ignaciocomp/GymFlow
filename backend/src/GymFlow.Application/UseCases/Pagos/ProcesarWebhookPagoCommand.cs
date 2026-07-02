using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Pagos;

/// <summary>
/// Resultado del procesamiento de un webhook de Mercado Pago.
/// </summary>
public enum WebhookResultado
{
    /// <summary>Firma HMAC inválida: se descartó sin tocar datos (CA-36). El endpoint responde 401.</summary>
    FirmaInvalida,
    /// <summary>Se aplicó un cambio real (cuota pagada / pago rechazado).</summary>
    Procesado,
    /// <summary>Sin cambios: pago desconocido, ya procesado (idempotente) o estado no terminal.</summary>
    Ignorado
}

/// <summary>
/// RF-23 / CU-08 — corazón de seguridad. Procesa la notificación (webhook) de Mercado Pago:
/// 1) valida la firma HMAC (RN-31); si es inválida, audita el evento sospechoso y descarta (CA-36).
/// 2) consulta el estado REAL del pago en la API de MP (no se confía en el payload).
/// 3) reconcilia contra el <see cref="Pago"/> por external_reference y aplica el resultado:
///    approved → marca la cuota Pagada + pago Aprobado (idempotente), audita y manda email (CA-34/35);
///    rejected → marca el pago Rechazado, cuota sin cambios (E1/CA-37).
/// </summary>
public class ProcesarWebhookPagoCommand
{
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly IPagoRepository _pagoRepository;
    private readonly ICuotaRepository _cuotaRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;

    public ProcesarWebhookPagoCommand(
        IMercadoPagoService mercadoPagoService,
        IPagoRepository pagoRepository,
        ICuotaRepository cuotaRepository,
        ISocioRepository socioRepository,
        IEmailService emailService,
        IAuditLogger auditLogger)
    {
        _mercadoPagoService = mercadoPagoService;
        _pagoRepository = pagoRepository;
        _cuotaRepository = cuotaRepository;
        _socioRepository = socioRepository;
        _emailService = emailService;
        _auditLogger = auditLogger;
    }

    /// <param name="esIpn">
    /// <c>true</c> cuando la notificación llegó en formato IPN legacy (<c>topic/id</c>).
    /// Las firmas de IPN no son validables con el secret (docs MP), así que se OMITE
    /// <see cref="IMercadoPagoService.ValidarFirma"/>. La seguridad se preserva igual:
    /// nunca se confía en el contenido de la notificación — el estado SIEMPRE se consulta
    /// a la API de MP con nuestro access token, y solo se actúa sobre un <see cref="Pago"/>
    /// cuyo external_reference (nuestro GUID) coincida. Un id forjado no existe en MP
    /// o no mapea a ningún Pago nuestro → Ignorado.
    /// </param>
    public async Task<WebhookResultado> ExecuteAsync(string dataId, string? xSignature, string? xRequestId, bool esIpn = false)
    {
        // 1) Seguridad: en el formato moderno, sin firma válida NUNCA se modifica nada (RN-31, CA-36).
        if (!esIpn && !_mercadoPagoService.ValidarFirma(xSignature, xRequestId, dataId))
        {
            await _auditLogger.LogAsync(
                Guid.Empty, "Sistema",
                TipoAccionAuditoria.Modificacion, "Pago", null,
                "Webhook MP con firma inválida (descartado)");
            return WebhookResultado.FirmaInvalida;
        }

        // 2) Fuente de verdad: se consulta el estado real del pago en MP.
        var info = await _mercadoPagoService.ObtenerPagoAsync(dataId);
        if (info is null || !Guid.TryParse(info.ExternalReference, out var pagoId))
            return WebhookResultado.Ignorado;

        // 3) Reconciliación con el Pago local (external_reference == Pago.Id).
        var pago = await _pagoRepository.GetByExternalReferenceAsync(pagoId);
        if (pago is null)
            return WebhookResultado.Ignorado;

        var cuota = await _cuotaRepository.GetByIdAsync(pago.CuotaId);
        if (cuota is null)
            return WebhookResultado.Ignorado;

        if (info.Estado == "approved")
        {
            // Idempotencia: si la cuota ya está pagada, no se reprocesa ni se re-manda email (E4).
            if (cuota.Estado == EstadoCuota.Pagada)
                return WebhookResultado.Ignorado;

            cuota.MarcarComoPagada();
            pago.MarcarAprobado(info.PaymentId, info.MedioPago);
            // Un solo SaveChanges: el DbContext scoped es compartido → commitea Cuota + Pago atómicamente.
            await _pagoRepository.SaveChangesAsync();

            await _auditLogger.LogAsync(
                Guid.Empty, "Sistema",
                TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
                $"Se marcó como pagada (vía Mercado Pago) la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");

            await EnviarEmailConfirmacionAsync(cuota);
            return WebhookResultado.Procesado;
        }

        if (info.Estado == "rejected")
        {
            pago.MarcarRechazado();
            await _pagoRepository.SaveChangesAsync();
            return WebhookResultado.Procesado;
        }

        // pending / otros estados no terminales: sin cambios.
        return WebhookResultado.Ignorado;
    }

    // Reutiliza la MISMA plantilla de "pago confirmado" que MarcarCuotaPagadaCommand.
    // Best-effort: un fallo del email no debe romper la confirmación del pago.
    private async Task EnviarEmailConfirmacionAsync(Cuota cuota)
    {
        try
        {
            var socio = cuota.Socio ?? await _socioRepository.GetByIdAsync(cuota.SocioId);
            if (socio is null || string.IsNullOrWhiteSpace(socio.Correo))
                return;

            var (asunto, cuerpo) = EmailTemplates.ConfirmacionPago(socio, cuota);
            await _emailService.EnviarAsync(socio.Correo, asunto, cuerpo);
        }
        catch
        {
            // Best-effort.
        }
    }
}
