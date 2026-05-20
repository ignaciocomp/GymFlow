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

        foreach (var inscripcion in inscripciones)
        {
            await _emailService.EnviarAsync(
                inscripcion.Socio.Correo,
                $"Clase cancelada: {clase.Nombre}",
                $"<p>Hola {inscripcion.Socio.Nombre},</p>" +
                $"<p>Te informamos que la clase <strong>{clase.Nombre}</strong> ha sido cancelada.</p>" +
                $"<p>Disculpá las molestias.</p>" +
                $"<p>— GymFlow</p>");
        }

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Clase", clase.Id,
            $"Se canceló la clase '{clase.Nombre}'. Se notificaron {inscripciones.Count()} socios inscriptos.");
    }
}
