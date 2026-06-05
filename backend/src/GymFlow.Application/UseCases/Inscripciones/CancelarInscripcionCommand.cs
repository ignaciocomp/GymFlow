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

        var eraListaEspera = inscripcion.EsListaEspera;

        inscripcion.Cancelar();
        await _inscripcionRepo.SaveChangesAsync();

        if (!eraListaEspera)
        {
            var primero = await _inscripcionRepo.GetPrimeroEnListaEsperaAsync(inscripcion.HorarioClaseId);
            if (primero != null)
            {
                primero.PromoverDeListaEspera();
                await _inscripcionRepo.SaveChangesAsync();

                if (primero.HorarioClase != null && primero.Socio != null)
                {
                    var (asunto, cuerpo) = InscripcionEmailTemplates.CupoLiberado(primero.Socio, primero.HorarioClase);
                    await _emailService.EnviarAsync(primero.Socio.Correo, asunto, cuerpo);
                }

                await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Modificacion,
                    "Inscripcion", primero.Id, "Promovido de lista de espera por cupo liberado");
            }
        }

        await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Baja,
            "Inscripcion", inscripcion.Id, "Inscripcion cancelada");
    }
}
