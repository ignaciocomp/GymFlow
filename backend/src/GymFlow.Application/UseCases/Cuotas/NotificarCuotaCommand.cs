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

    public NotificarCuotaCommand(
        ICuotaRepository cuotaRepository,
        ISocioRepository socioRepository,
        IRecordatorioCuotaRepository recordatorioRepository,
        IEmailService emailService,
        IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _socioRepository = socioRepository;
        _recordatorioRepository = recordatorioRepository;
        _emailService = emailService;
        _auditLogger = auditLogger;
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

        if (await _recordatorioRepository.ExisteRecordatorioHoyAsync(cuotaId, TipoRecordatorio.Manual))
            throw new InvalidOperationException("Ya se envió un recordatorio manual a este socio por esta cuota hoy.");

        var asunto = $"Recordatorio: tu cuota de {cuota.NombrePlan} está pendiente";
        var cuerpo = ConstruirCuerpoEmail(socio, cuota);

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

        if (!resultado.Exitoso)
            throw new InvalidOperationException($"No se pudo enviar el email: {resultado.Error}");
    }

    private static string ConstruirCuerpoEmail(Socio socio, Cuota cuota)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Hola {socio.Nombre},</h2>
                <p>Te recordamos que tenés una cuota pendiente en GymFlow.</p>
                <table style='border-collapse: collapse;'>
                    <tr><td><b>Plan:</b></td><td>{cuota.NombrePlan}</td></tr>
                    <tr><td><b>Unidad:</b></td><td>{cuota.Unidad?.Nombre}</td></tr>
                    <tr><td><b>Monto:</b></td><td>${cuota.Monto:N2}</td></tr>
                    <tr><td><b>Vencimiento:</b></td><td>{cuota.FechaVencimiento:dd/MM/yyyy}</td></tr>
                </table>
                <p>Por favor regularizá tu pago a la brevedad.</p>
                <p>Saludos,<br/>Equipo GymFlow</p>
            </body>
            </html>";
    }
}
