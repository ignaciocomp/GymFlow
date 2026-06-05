using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Clases;

public class CancelClaseCommand
{
    private readonly IClaseRepository _claseRepository;
    private readonly IHorarioClaseRepository _horarioRepository;
    private readonly IInscripcionClaseRepository _inscripcionRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;

    public CancelClaseCommand(
        IClaseRepository claseRepository,
        IHorarioClaseRepository horarioRepository,
        IInscripcionClaseRepository inscripcionRepository,
        IAuditLogger auditLogger,
        IEmailService emailService)
    {
        _claseRepository = claseRepository;
        _horarioRepository = horarioRepository;
        _inscripcionRepository = inscripcionRepository;
        _auditLogger = auditLogger;
        _emailService = emailService;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("La clase no fue encontrada.");

        clase.Cancelar();

        var horarios = await _horarioRepository.GetByClaseIdAsync(id);
        var inscripciones = new List<InscripcionClase>();
        foreach (var horario in horarios)
        {
            var inscripcionesHorario = await _inscripcionRepository.GetActivasByHorarioClaseIdAsync(horario.Id);
            inscripciones.AddRange(inscripcionesHorario);
        }

        foreach (var inscripcion in inscripciones)
        {
            inscripcion.Cancelar();
        }

        await _claseRepository.SaveChangesAsync();

        var emailTasks = inscripciones.Select(inscripcion =>
        {
            var (asunto, cuerpo) = ClaseEmailTemplates.Cancelacion(inscripcion.Socio, clase);
            return _emailService.EnviarAsync(inscripcion.Socio.Correo, asunto, cuerpo);
        });

        var resultados = await Task.WhenAll(emailTasks);
        var enviados = resultados.Count(r => r.Exitoso);
        var fallidos = resultados.Length - enviados;

        var detalle = fallidos > 0
            ? $"Se cancelo la clase '{clase.Nombre}'. Se notificaron {enviados} de {resultados.Length} socios ({fallidos} envios fallaron)."
            : $"Se cancelo la clase '{clase.Nombre}'. Se notificaron {enviados} socios inscriptos.";

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Clase", clase.Id,
            detalle);
    }
}
