using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

public class InscribirSocioCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IClaseRepository _claseRepo;

    public InscribirSocioCommand(IInscripcionClaseRepository inscripcionRepo, IClaseRepository claseRepo)
    {
        _inscripcionRepo = inscripcionRepo;
        _claseRepo = claseRepo;
    }

    public async Task<InscripcionClaseDto> ExecuteAsync(Guid socioId, Guid claseId)
    {
        var clase = await _claseRepo.GetByIdAsync(claseId)
            ?? throw new KeyNotFoundException("La clase no existe.");

        if (!clase.EstaActivo)
            throw new InvalidOperationException("No se puede inscribir a una clase cancelada.");

        var existente = await _inscripcionRepo.GetActivaBySocioYClaseAsync(socioId, claseId);
        if (existente != null)
            throw new InvalidOperationException("Ya estás inscripto en esta clase.");

        var inscripcionesActivas = await _inscripcionRepo.GetInscripcionesActivasCountAsync(claseId);
        if (inscripcionesActivas >= clase.CapacidadMaxima)
            throw new InvalidOperationException("La clase está llena. No hay cupo disponible.");

        var inscripcion = new InscripcionClase(claseId, socioId);
        await _inscripcionRepo.AddAsync(inscripcion);
        await _inscripcionRepo.SaveChangesAsync();

        return InscripcionMapper.ToDto(inscripcion, clase, inscripcionesActivas + 1);
    }
}
