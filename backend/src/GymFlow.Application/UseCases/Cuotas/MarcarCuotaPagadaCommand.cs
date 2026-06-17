using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class MarcarCuotaPagadaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;

    public MarcarCuotaPagadaCommand(
        ICuotaRepository cuotaRepository,
        ISocioRepository socioRepository,
        IEmailService emailService,
        IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _socioRepository = socioRepository;
        _emailService = emailService;
        _auditLogger = auditLogger;
    }

    public async Task<CuotaDto> ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        cuota.MarcarComoPagada();
        await _cuotaRepository.SaveChangesAsync();

        // Email de confirmación best-effort: si el envío falla, el pago igual queda registrado.
        await EnviarEmailConfirmacionAsync(cuota);

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
            $"Se marcó como pagada la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");

        return GetCuotasBySocioQuery.MapToDto(cuota);
    }

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
            // Best-effort: un fallo del email no debe romper la confirmación del pago.
        }
    }
}
