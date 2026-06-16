using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Clases;

public class UpdateClaseCommand
{
    private readonly IClaseRepository _claseRepository;
    private readonly IHorarioClaseRepository _horarioRepository;
    private readonly IInscripcionClaseRepository _inscripcionRepository;
    private readonly IAuditLogger _auditLogger;

    public UpdateClaseCommand(
        IClaseRepository claseRepository,
        IHorarioClaseRepository horarioRepository,
        IInscripcionClaseRepository inscripcionRepository,
        IAuditLogger auditLogger)
    {
        _claseRepository = claseRepository;
        _horarioRepository = horarioRepository;
        _inscripcionRepository = inscripcionRepository;
        _auditLogger = auditLogger;
    }

    public async Task<ClaseDto> ExecuteAsync(Guid id, UpdateClaseRequest request, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("La clase no fue encontrada.");

        var horarios = await _horarioRepository.GetByClaseIdAsync(id);
        var horarioIds = horarios.Select(h => h.Id);
        var conteos = await _inscripcionRepository.GetConteoActivasPorHorariosAsync(horarioIds);
        var maxInscripciones = conteos.Values.DefaultIfEmpty(0).Max();

        clase.Actualizar(request.Nombre, request.Descripcion ?? "", request.CapacidadMaxima,
            request.DuracionMinutos, request.Instructor, maxInscripciones);

        await _claseRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Modificacion, "Clase", clase.Id,
            $"Se actualizo la clase '{clase.Nombre}'");

        var totalInscripciones = conteos.Values.Sum();
        return new ClaseDto(clase.Id, clase.Nombre, clase.Descripcion, clase.CapacidadMaxima, clase.DuracionMinutos,
            clase.Instructor, clase.UnidadId, clase.Unidad?.Nombre ?? "", clase.EstaActivo, totalInscripciones);
    }
}
