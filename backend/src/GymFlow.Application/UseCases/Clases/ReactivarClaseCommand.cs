using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Clases;

public class ReactivarClaseCommand
{
    private readonly IClaseRepository _claseRepository;
    private readonly IAuditLogger _auditLogger;

    public ReactivarClaseCommand(IClaseRepository claseRepository, IAuditLogger auditLogger)
    {
        _claseRepository = claseRepository;
        _auditLogger = auditLogger;
    }

    public async Task<ClaseDto> ExecuteAsync(Guid id, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("La clase no fue encontrada.");

        clase.Reactivar();
        await _claseRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Reactivacion, "Clase", clase.Id,
            $"Se reactivó la clase '{clase.Nombre}'");

        return new ClaseDto(clase.Id, clase.Nombre, clase.Descripcion, clase.CapacidadMaxima, clase.DuracionMinutos,
            clase.Instructor, clase.UnidadId, clase.Unidad?.Nombre ?? "", clase.EstaActivo);
    }
}
