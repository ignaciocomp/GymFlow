using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Portal;

public class SolicitarModificacionCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IAuditLogger _auditLogger;

    public SolicitarModificacionCommand(ISocioRepository socioRepository, IAuditLogger auditLogger)
    {
        _socioRepository = socioRepository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(string correo, string detalle, Guid usuarioId, string usuarioNombre)
    {
        if (string.IsNullOrWhiteSpace(detalle))
            throw new ArgumentException("El detalle de la solicitud es obligatorio.");

        var socio = await _socioRepository.GetByCorreoAsync(correo)
            ?? throw new KeyNotFoundException("No se encontró el socio.");

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.SolicitudModificacion, "Socio", socio.Id,
            $"{usuarioNombre} solicitó modificación de datos: {detalle}");
    }
}
