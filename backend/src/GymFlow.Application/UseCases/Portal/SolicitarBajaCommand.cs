using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Portal;

public class SolicitarBajaCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IAuditLogger _auditLogger;

    public SolicitarBajaCommand(ISocioRepository socioRepository, IAuditLogger auditLogger)
    {
        _socioRepository = socioRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(string correo, string? motivo, Guid usuarioId, string usuarioNombre)
    {
        var socio = await _socioRepository.GetByCorreoAsync(correo)
            ?? throw new KeyNotFoundException("No se encontró el socio.");

        var descripcion = string.IsNullOrWhiteSpace(motivo)
            ? $"{usuarioNombre} solicitó la baja de su cuenta."
            : $"{usuarioNombre} solicitó la baja de su cuenta. Motivo: {motivo}";

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.SolicitudBaja, "Socio", socio.Id,
            descripcion);
    }
}
