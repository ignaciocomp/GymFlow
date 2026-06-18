using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

/// <summary>
/// RF-06: el admin envía manualmente un recordatorio por email a un socio
/// por una cuota pendiente específica.
/// </summary>
public class NotificarCuotaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IRecordatorioCuotaRepository _recordatorioRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;
    private readonly INotificadorInApp _notificador;

    public NotificarCuotaCommand(
        ICuotaRepository cuotaRepository,
        ISocioRepository socioRepository,
        IRecordatorioCuotaRepository recordatorioRepository,
        IEmailService emailService,
        IAuditLogger auditLogger,
        INotificadorInApp notificador)
    {
        _cuotaRepository = cuotaRepository;
        _socioRepository = socioRepository;
        _recordatorioRepository = recordatorioRepository;
        _emailService = emailService;
        _auditLogger = auditLogger;
        _notificador = notificador;
    }

    public async Task ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        if (cuota.Estado != EstadoCuota.Pendiente)
            throw new InvalidOperationException("Solo se pueden notificar cuotas en estado pendiente.");

        var socio = await _socioRepository.GetByIdAsync(cuota.SocioId)
            ?? throw new KeyNotFoundException("El socio asociado a la cuota no existe.");

        if (string.IsNullOrWhiteSpace(socio.Correo))
            throw new InvalidOperationException("El socio no tiene correo registrado.");

        // Bloquear sólo si ya hay un recordatorio manual EXITOSO hoy.
        // Si el último intento falló (timeout SMTP, etc.), permitimos reintentar.
        if (await _recordatorioRepository.ExisteRecordatorioExitosoHoyAsync(cuotaId, TipoRecordatorio.Manual))
            throw new InvalidOperationException("Ya se envió un recordatorio manual exitoso a este socio por esta cuota hoy.");

        var (asunto, cuerpo) = EmailTemplates.Manual(socio, cuota);

        var resultado = await _emailService.EnviarAsync(socio.Correo, asunto, cuerpo);

        var recordatorio = new RecordatorioCuota(
            cuotaId: cuota.Id,
            socioId: socio.Id,
            tipo: TipoRecordatorio.Manual,
            exitoso: resultado.Exitoso,
            error: resultado.Error);

        await _recordatorioRepository.AddAsync(recordatorio);
        await _recordatorioRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
            $"Recordatorio manual enviado a {socio.Nombre} {socio.Apellido} ({socio.Correo}) por la cuota de {cuota.NombrePlan}. Resultado: {(resultado.Exitoso ? "exitoso" : $"fallido — {resultado.Error}")}");

        // Notificación in-app solo en la rama de éxito (no si el mail falla y el flujo termina en 500).
        // Best-effort: si crear la notificación falla, no rompe el envío del recordatorio.
        if (resultado.Exitoso)
        {
            try
            {
                await _notificador.CrearAsync(
                    socio.Id,
                    TipoNotificacion.RecordatorioCuota,
                    $"Recordatorio: tu cuota de {cuota.NombrePlan} está pendiente",
                    $"Tenés una cuota pendiente de {cuota.NombrePlan} por ${cuota.Monto:N2} con vencimiento {cuota.FechaVencimiento:dd/MM/yyyy}. Por favor regularizá tu pago a la brevedad.");
            }
            catch
            {
                // Best-effort: la creación de la notificación in-app nunca rompe la operación.
            }
        }

        if (!resultado.Exitoso)
            throw new InvalidOperationException("No se pudo enviar el email. Reintentá más tarde.");
    }
}
