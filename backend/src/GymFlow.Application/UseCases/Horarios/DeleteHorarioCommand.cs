using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Horarios;

public class DeleteHorarioCommand
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IAuditLogger _auditLogger;

    public DeleteHorarioCommand(IHorarioClaseRepository horarioRepo, IAuditLogger auditLogger)
    {
        _horarioRepo = horarioRepo;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var horario = await _horarioRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El horario no fue encontrado.");

        var descripcion = $"Se eliminó horario de '{horario.Clase?.Nombre}': {horario.DiaSemana} {horario.HoraInicio:HH:mm}-{horario.HoraFin:HH:mm}";

        _horarioRepo.Remove(horario);
        await _horarioRepo.SaveChangesAsync();

        await _auditLogger.LogAsync(usuarioId, usuarioNombre,
            TipoAccionAuditoria.Baja, "Horario", id, descripcion);
    }
}
