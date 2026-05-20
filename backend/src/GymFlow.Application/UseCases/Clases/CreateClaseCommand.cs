using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Clases;

public class CreateClaseCommand
{
    private readonly IClaseRepository _claseRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IAuditLogger _auditLogger;

    public CreateClaseCommand(IClaseRepository claseRepository, IUnidadRepository unidadRepository, IAuditLogger auditLogger)
    {
        _claseRepository = claseRepository;
        _unidadRepository = unidadRepository;
        _auditLogger = auditLogger;
    }

    public async Task<ClaseDto> ExecuteAsync(CreateClaseRequest request, Guid usuarioId, string usuarioNombre)
    {
        var unidad = await _unidadRepository.GetByIdAsync(request.UnidadId)
            ?? throw new ArgumentException("La unidad seleccionada no existe.");

        var clase = new Clase(request.Nombre, request.Descripcion ?? "", request.CapacidadMaxima,
            request.DuracionMinutos, request.Instructor, request.UnidadId);

        await _claseRepository.AddAsync(clase);
        await _claseRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Clase", clase.Id,
            $"Se creó la clase '{request.Nombre}' en {unidad.Nombre}");

        return new ClaseDto(clase.Id, clase.Nombre, clase.Descripcion, clase.CapacidadMaxima, clase.DuracionMinutos,
            clase.Instructor, clase.UnidadId, unidad.Nombre, clase.EstaActivo, 0);
    }
}
