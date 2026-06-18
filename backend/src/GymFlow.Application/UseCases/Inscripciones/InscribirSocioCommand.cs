using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Inscripciones;

public class InscribirSocioCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly ISocioRepository _socioRepo;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;
    private readonly INotificadorInApp _notificador;

    public InscribirSocioCommand(
        IInscripcionClaseRepository inscripcionRepo,
        IHorarioClaseRepository horarioRepo,
        ISocioRepository socioRepo,
        IEmailService emailService,
        IAuditLogger auditLogger,
        INotificadorInApp notificador)
    {
        _inscripcionRepo = inscripcionRepo;
        _horarioRepo = horarioRepo;
        _socioRepo = socioRepo;
        _emailService = emailService;
        _auditLogger = auditLogger;
        _notificador = notificador;
    }

    public async Task<InscripcionClaseDto> ExecuteAsync(Guid socioId, Guid horarioClaseId, Guid usuarioId, string usuarioNombre)
    {
        var horario = await _horarioRepo.GetByIdAsync(horarioClaseId)
            ?? throw new KeyNotFoundException("El horario no existe.");
        var clase = horario.Clase;

        if (!clase.EstaActivo)
            throw new InvalidOperationException("No se puede inscribir a una clase cancelada.");

        var existente = await _inscripcionRepo.GetActivaBySocioYHorarioAsync(socioId, horarioClaseId);
        if (existente != null)
            throw new InvalidOperationException("Ya estas inscripto en este horario.");

        var ocupados = await _inscripcionRepo.GetInscripcionesActivasCountAsync(horarioClaseId);
        if (ocupados >= clase.CapacidadMaxima)
            throw new InvalidOperationException("No hay cupos disponibles para este horario.");

        var inscripcion = new InscripcionClase(horarioClaseId, socioId);
        await _inscripcionRepo.AddAsync(inscripcion);
        await _inscripcionRepo.SaveChangesAsync();

        var socio = await _socioRepo.GetByIdAsync(socioId);

        if (socio != null)
        {
            var (asunto, cuerpo) = InscripcionEmailTemplates.Confirmacion(socio, horario);
            await _emailService.EnviarAsync(socio.Correo, asunto, cuerpo);
        }

        // Notificación in-app tras el save de negocio. Best-effort: si la creación falla,
        // la inscripción igual queda confirmada.
        try
        {
            await _notificador.CrearAsync(
                socioId,
                TipoNotificacion.ConfirmacionInscripcion,
                $"Inscripción confirmada: {clase.Nombre}",
                $"Tu inscripción a la clase {clase.Nombre} ({horario.DiaSemana} {horario.HoraInicio:HH:mm} - {horario.HoraFin:HH:mm}) fue confirmada.");
        }
        catch
        {
            // Best-effort: la creación de la notificación in-app nunca rompe la inscripción.
        }

        await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Creacion,
            "Inscripcion", inscripcion.Id,
            $"Socio inscripto en la clase '{clase.Nombre}' en horario {horario.DiaSemana} {horario.HoraInicio:HH:mm}.");

        return InscripcionMapper.ToDto(inscripcion, horario, ocupados + 1);
    }
}
