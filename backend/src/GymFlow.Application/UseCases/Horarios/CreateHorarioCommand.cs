using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Horarios;

public class CreateHorarioCommand
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IClaseRepository _claseRepo;
    private readonly IAuditLogger _auditLogger;

    public CreateHorarioCommand(IHorarioClaseRepository horarioRepo, IClaseRepository claseRepo, IAuditLogger auditLogger)
    {
        _horarioRepo = horarioRepo;
        _claseRepo = claseRepo;
        _auditLogger = auditLogger;
    }

    public async Task<HorarioClaseDto> ExecuteAsync(CreateHorarioClaseRequest request, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepo.GetByIdAsync(request.ClaseId)
            ?? throw new ArgumentException("La clase seleccionada no existe.");

        if (!clase.EstaActivo)
            throw new InvalidOperationException("No se puede agregar horarios a una clase cancelada.");

        if (!TimeOnly.TryParse(request.HoraInicio, out var horaInicio))
            throw new ArgumentException("La hora de inicio no es válida. Use formato HH:mm.");
        if (!TimeOnly.TryParse(request.HoraFin, out var horaFin))
            throw new ArgumentException("La hora de fin no es válida. Use formato HH:mm.");

        var horario = new HorarioClase(request.ClaseId, request.DiaSemana, horaInicio, horaFin, request.Sala);

        // Validar conflicto de sala
        await ValidarConflictoSala(horario, clase.UnidadId);

        await _horarioRepo.AddAsync(horario);
        await _horarioRepo.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Horario", horario.Id,
            $"Se creó horario para '{clase.Nombre}': {request.DiaSemana} {request.HoraInicio}-{request.HoraFin}" +
            (request.Sala != null ? $" en sala {request.Sala}" : ""));

        return HorarioMapper.ToDto(horario, 0);
    }

    private async Task ValidarConflictoSala(HorarioClase nuevoHorario, Guid unidadId)
    {
        if (nuevoHorario.Sala == null) return;

        var horariosDelDia = await _horarioRepo.GetByUnidadYDiaAsync(unidadId, nuevoHorario.DiaSemana);
        foreach (var existente in horariosDelDia)
        {
            if (existente.Id == nuevoHorario.Id) continue;
            if (nuevoHorario.SeSolapaCon(existente))
            {
                throw new InvalidOperationException(
                    $"Conflicto de sala: la sala '{nuevoHorario.Sala}' ya está ocupada el {nuevoHorario.DiaSemana} " +
                    $"de {existente.HoraInicio:HH:mm} a {existente.HoraFin:HH:mm} por la clase '{existente.Clase?.Nombre}'.");
            }
        }
    }
}
