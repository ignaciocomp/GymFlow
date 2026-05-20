using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Clases;

public class UpdateClaseCommand
{
    private readonly IClaseRepository _claseRepository;
    private readonly IAuditLogger _auditLogger;

    public UpdateClaseCommand(IClaseRepository claseRepository, IAuditLogger auditLogger)
    {
        _claseRepository = claseRepository;
        _auditLogger = auditLogger;
    }

    public async Task<ClaseDto> ExecuteAsync(Guid id, UpdateClaseRequest request, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("La clase no fue encontrada.");

        var inscripcionesActivas = await _claseRepository.GetInscripcionesActivasCountAsync(id);

        clase.Actualizar(request.Nombre, request.Descripcion ?? "", request.CapacidadMaxima,
            request.DuracionMinutos, request.Instructor, inscripcionesActivas);

        await _claseRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Clase", clase.Id,
            $"Se actualizó la clase '{clase.Nombre}'");

        return new ClaseDto(clase.Id, clase.Nombre, clase.Descripcion, clase.CapacidadMaxima, clase.DuracionMinutos,
            clase.Instructor, clase.UnidadId, clase.Unidad?.Nombre ?? "", clase.EstaActivo, inscripcionesActivas);
    }
}
