using System.Net;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Horarios;

public class UpdateHorarioCommand
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IClaseRepository _claseRepo;
    private readonly IAuditLogger _auditLogger;
    private readonly IEmailService _emailService;

    public UpdateHorarioCommand(
        IHorarioClaseRepository horarioRepo,
        IClaseRepository claseRepo,
        IAuditLogger auditLogger,
        IEmailService emailService)
    {
        _horarioRepo = horarioRepo;
        _claseRepo = claseRepo;
        _auditLogger = auditLogger;
        _emailService = emailService;
    }

    public async Task<HorarioClaseDto> ExecuteAsync(Guid id, UpdateHorarioClaseRequest request, Guid usuarioId, string usuarioNombre)
    {
        var horario = await _horarioRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El horario no fue encontrado.");

        if (!TimeOnly.TryParse(request.HoraInicio, out var horaInicio))
            throw new ArgumentException("La hora de inicio no es válida. Use formato HH:mm.");
        if (!TimeOnly.TryParse(request.HoraFin, out var horaFin))
            throw new ArgumentException("La hora de fin no es válida. Use formato HH:mm.");

        var diaAnterior = horario.DiaSemana;
        var inicioAnterior = horario.HoraInicio;
        var finAnterior = horario.HoraFin;

        horario.Actualizar(request.DiaSemana, horaInicio, horaFin, request.Sala);

        // Validar conflicto de sala
        await ValidarConflictoSala(horario, horario.Clase.UnidadId);

        await _horarioRepo.SaveChangesAsync();

        // Notificar inscriptos si el horario cambió
        var inscripciones = await _claseRepo.GetInscripcionesActivasAsync(horario.ClaseId);
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
                ? $"Se modificó horario de '{horario.Clase.Nombre}'. Se notificaron {enviados} de {resultados.Length} socios ({fallidos} fallaron)."
                : $"Se modificó horario de '{horario.Clase.Nombre}'. Se notificaron {enviados} socios inscriptos.";

            await _auditLogger.LogAsync(usuarioId, usuarioNombre,
                TipoAccionAuditoria.Modificacion, "Horario", horario.Id, detalle);
        }
        else
        {
            await _auditLogger.LogAsync(usuarioId, usuarioNombre,
                TipoAccionAuditoria.Modificacion, "Horario", horario.Id,
                $"Se modificó horario de '{horario.Clase.Nombre}': {request.DiaSemana} {request.HoraInicio}-{request.HoraFin}");
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
                    $"Conflicto de sala: la sala '{horario.Sala}' ya está ocupada el {horario.DiaSemana} " +
                    $"de {existente.HoraInicio:HH:mm} a {existente.HoraFin:HH:mm} por la clase '{existente.Clase?.Nombre}'.");
            }
        }
    }
}
