using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Inscripciones;

public class CancelarInscripcionCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;

    public CancelarInscripcionCommand(
        IInscripcionClaseRepository inscripcionRepo,
        IEmailService emailService,
        IAuditLogger auditLogger)
    {
        _inscripcionRepo = inscripcionRepo;
        _emailService = emailService;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid inscripcionId, Guid socioId, Guid usuarioId, string usuarioNombre)
    {
        var inscripcion = await _inscripcionRepo.GetByIdAsync(inscripcionId)
            ?? throw new KeyNotFoundException("La inscripcion no existe.");

        if (inscripcion.SocioId != socioId)
            throw new InvalidOperationException("No tenes permiso para cancelar esta inscripcion.");

        if (!inscripcion.EstaActiva)
            throw new InvalidOperationException("La inscripcion ya fue cancelada.");

        inscripcion.Cancelar();
        await _inscripcionRepo.SaveChangesAsync();

        await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Baja,
            "Inscripcion", inscripcion.Id, "Inscripcion cancelada");
    }
}
