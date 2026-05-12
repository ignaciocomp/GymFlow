using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class MarcarCuotaPagadaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IAuditLogger _auditLogger;

    public MarcarCuotaPagadaCommand(ICuotaRepository cuotaRepository, IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        cuota.MarcarComoPagada();
        await _cuotaRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
            $"Se marcó como pagada la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");
    }
}
