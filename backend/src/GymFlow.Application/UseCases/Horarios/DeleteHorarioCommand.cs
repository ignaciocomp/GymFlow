using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Horarios;

public class DeleteHorarioCommand
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;
    private readonly INotificadorInApp _notificador;

    public DeleteHorarioCommand(
        IHorarioClaseRepository horarioRepo,
        IInscripcionClaseRepository inscripcionRepo,
        IAuditLogger auditLogger,
        IEmailService emailService,
        INotificadorInApp notificador)
    {
        _horarioRepo = horarioRepo;
        _inscripcionRepo = inscripcionRepo;
        _auditLogger = auditLogger;
        _emailService = emailService;
        _notificador = notificador;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var horario = await _horarioRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El horario no fue encontrado.");

        var descripcion = $"Se eliminó horario de '{horario.Clase?.Nombre}': {horario.DiaSemana} {horario.HoraInicio:HH:mm}-{horario.HoraFin:HH:mm}";

        // E2E-05: la FK InscripcionClase → HorarioClase borra en cascada; sin esta guarda,
        // los inscriptos perdían su inscripción en silencio. Se cancelan (soft) y se
        // notifica (email + in-app) ANTES de eliminar el horario.
        var inscripciones = (await _inscripcionRepo.GetActivasByHorarioClaseIdAsync(id)).ToList();

        if (inscripciones.Count > 0)
        {
            foreach (var inscripcion in inscripciones)
                inscripcion.Cancelar();

            // Persistir las cancelaciones antes de remover el horario, para que la cascade
            // no interfiera con el soft-cancel ni con el conteo de envíos.
            await _inscripcionRepo.SaveChangesAsync();

            var emailTasks = inscripciones.Select(inscripcion =>
            {
                var (asunto, cuerpo) = HorarioEmailTemplates.EliminacionHorario(
                    inscripcion.Socio, horario.Clase!, horario);
                return _emailService.EnviarAsync(inscripcion.Socio.Correo, asunto, cuerpo);
            });

            var resultados = await Task.WhenAll(emailTasks);
            var enviados = resultados.Count(r => r.Exitoso);
            var fallidos = resultados.Length - enviados;

            descripcion = fallidos > 0
                ? $"{descripcion}. Se cancelaron {inscripciones.Count} inscripciones y se notificaron {enviados} de {resultados.Length} socios ({fallidos} envíos fallaron)."
                : $"{descripcion}. Se cancelaron {inscripciones.Count} inscripciones y se notificaron {enviados} socios inscriptos.";

            // Notificación in-app a los inscriptos. Best-effort: si la creación falla,
            // el borrado igual se completa.
            try
            {
                var socioIds = inscripciones.Select(i => i.SocioId).Distinct();
                await _notificador.CrearParaVariosAsync(
                    socioIds,
                    TipoNotificacion.CancelacionClase,
                    $"Horario eliminado: {horario.Clase?.Nombre}",
                    $"El horario de la clase {horario.Clase?.Nombre} ({horario.DiaSemana} {horario.HoraInicio:HH:mm} - {horario.HoraFin:HH:mm}) fue eliminado; tu inscripción quedó sin efecto.");
            }
            catch
            {
                // Best-effort: la creación de las notificaciones in-app nunca rompe el borrado.
            }
        }

        _horarioRepo.Remove(horario);
        await _horarioRepo.SaveChangesAsync();

        await _auditLogger.LogAsync(usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Horario", id, descripcion);
    }
}
