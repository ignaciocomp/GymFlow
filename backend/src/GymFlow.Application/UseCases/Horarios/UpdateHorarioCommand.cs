using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Horarios;

public class UpdateHorarioCommand
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;
    private readonly INotificadorInApp _notificador;

    public UpdateHorarioCommand(
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

    public async Task<HorarioClaseDto> ExecuteAsync(Guid id, UpdateHorarioClaseRequest request, Guid usuarioId, string usuarioNombre)
    {
        var horario = await _horarioRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El horario no fue encontrado.");

        if (!TimeOnly.TryParse(request.HoraInicio, out var horaInicio))
            throw new ArgumentException("La hora de inicio no es valida. Use formato HH:mm.");
        if (!TimeOnly.TryParse(request.HoraFin, out var horaFin))
            throw new ArgumentException("La hora de fin no es valida. Use formato HH:mm.");

        var diaAnterior = horario.DiaSemana;
        var inicioAnterior = horario.HoraInicio;
        var finAnterior = horario.HoraFin;

        horario.Actualizar(request.DiaSemana, horaInicio, horaFin, request.Sala);

        await ValidarConflictoSala(horario, horario.Clase.UnidadId);

        await _horarioRepo.SaveChangesAsync();

        var inscripciones = await _inscripcionRepo.GetActivasByHorarioClaseIdAsync(horario.Id);
        var inscripcionesActivas = inscripciones.Count();

        if (inscripcionesActivas > 0)
        {
            var emailTasks = inscripciones.Select(insc =>
            {
                var (asunto, cuerpo) = HorarioEmailTemplates.CambioHorario(
                    insc.Socio, horario.Clase, diaAnterior, inicioAnterior, finAnterior, horario);
                return _emailService.EnviarAsync(insc.Socio.Correo, asunto, cuerpo);
            });

            var resultados = await Task.WhenAll(emailTasks);
            var enviados = resultados.Count(r => r.Exitoso);
            var fallidos = resultados.Length - enviados;

            var detalle = fallidos > 0
                ? $"Se modifico horario de '{horario.Clase.Nombre}'. Se notificaron {enviados} de {resultados.Length} socios ({fallidos} fallaron)."
                : $"Se modifico horario de '{horario.Clase.Nombre}'. Se notificaron {enviados} socios inscriptos.";

            await _auditLogger.LogAsync(usuarioId, usuarioNombre,
                TipoAccionAuditoria.Modificacion, "Horario", horario.Id, detalle);

            // Notificación in-app a los inscriptos tras el save de negocio. Best-effort:
            // si la creación falla, el cambio de horario igual queda confirmado.
            try
            {
                var socioIds = inscripciones.Select(insc => insc.SocioId);
                await _notificador.CrearParaVariosAsync(
                    socioIds,
                    TipoNotificacion.CambioHorario,
                    $"Cambio de horario: {horario.Clase.Nombre}",
                    $"El horario de la clase {horario.Clase.Nombre} cambió a {horario.DiaSemana} de {horario.HoraInicio:HH:mm} a {horario.HoraFin:HH:mm}.");
            }
            catch
            {
                // Best-effort: la creación de las notificaciones in-app nunca rompe el cambio de horario.
            }
        }
        else
        {
            await _auditLogger.LogAsync(usuarioId, usuarioNombre,
                TipoAccionAuditoria.Modificacion, "Horario", horario.Id,
                $"Se modifico horario de '{horario.Clase.Nombre}': {request.DiaSemana} {request.HoraInicio}-{request.HoraFin}");
        }

        return HorarioMapper.ToDto(horario, inscripcionesActivas);
    }

    private async Task ValidarConflictoSala(HorarioClase horario, Guid unidadId)
    {
        if (horario.Sala == null) return;

        var horariosDelDia = await _horarioRepo.GetByUnidadYDiaAsync(unidadId, horario.DiaSemana);
        foreach (var existente in horariosDelDia)
        {
            if (existente.Id == horario.Id) continue;
            if (horario.SeSolapaCon(existente))
            {
                throw new InvalidOperationException(
                    $"Conflicto de sala: la sala '{horario.Sala}' ya esta ocupada el {horario.DiaSemana} " +
                    $"de {existente.HoraInicio:HH:mm} a {existente.HoraFin:HH:mm} por la clase '{existente.Clase?.Nombre}'.");
            }
        }
    }
}
