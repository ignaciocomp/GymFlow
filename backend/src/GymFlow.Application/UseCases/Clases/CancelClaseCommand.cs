using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Clases;

public class CancelClaseCommand
{
    private readonly IClaseRepository _claseRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;

    public CancelClaseCommand(IClaseRepository claseRepository, IAuditLogger auditLogger, IEmailService emailService)
    {
        _claseRepository = claseRepository;
        _auditLogger = auditLogger;
        _emailService = emailService;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("La clase no fue encontrada.");

        clase.Cancelar();

        var inscripciones = await _claseRepository.GetInscripcionesActivasAsync(id);
        foreach (var inscripcion in inscripciones)
        {
            inscripcion.Cancelar();
        }

        await _claseRepository.SaveChangesAsync();

        // Enviar notificaciones en paralelo para no bloquear el request
        var emailTasks = inscripciones.Select(inscripcion =>
        {
            var (asunto, cuerpo) = ClaseEmailTemplates.Cancelacion(inscripcion.Socio, clase);
            return _emailService.EnviarAsync(inscripcion.Socio.Correo, asunto, cuerpo);
        });

        var resultados = await Task.WhenAll(emailTasks);
        var enviados = resultados.Count(r => r.Exitoso);
        var fallidos = resultados.Length - enviados;

        var detalle = fallidos > 0
            ? $"Se canceló la clase '{clase.Nombre}'. Se notificaron {enviados} de {resultados.Length} socios ({fallidos} envíos fallaron)."
            : $"Se canceló la clase '{clase.Nombre}'. Se notificaron {enviados} socios inscriptos.";

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Clase", clase.Id,
            detalle);
    }
}
