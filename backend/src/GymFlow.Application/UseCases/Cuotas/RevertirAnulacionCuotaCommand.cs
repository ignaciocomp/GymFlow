using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class RevertirAnulacionCuotaCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IAuditLogger _auditLogger;

    public RevertirAnulacionCuotaCommand(ICuotaRepository cuotaRepository, IAuditLogger auditLogger)
    {
        _cuotaRepository = cuotaRepository;
        _auditLogger = auditLogger;
    }

    public async Task<CuotaDto> ExecuteAsync(Guid cuotaId, Guid usuarioId, string usuarioNombre)
    {
        var cuota = await _cuotaRepository.GetByIdAsync(cuotaId)
            ?? throw new KeyNotFoundException("La cuota no fue encontrada.");

        cuota.RevertirAnulacion();
        await _cuotaRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Cuota", cuota.Id,
            $"Se revirtió la anulación de la cuota de {cuota.NombrePlan} del socio {cuota.Socio?.Nombre} {cuota.Socio?.Apellido}");

        return GetCuotasBySocioQuery.MapToDto(cuota);
    }
}
