using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Inscripciones;

public class CancelarInscripcionCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public CancelarInscripcionCommand(IInscripcionClaseRepository inscripcionRepo)
    {
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task ExecuteAsync(Guid inscripcionId, Guid socioId)
    {
        var inscripcion = await _inscripcionRepo.GetByIdAsync(inscripcionId)
            ?? throw new KeyNotFoundException("La inscripción no existe.");

        if (inscripcion.SocioId != socioId)
            throw new InvalidOperationException("No tenés permiso para cancelar esta inscripción.");

        if (!inscripcion.EstaActiva)
            throw new InvalidOperationException("La inscripción ya fue cancelada.");

        inscripcion.Cancelar();
        await _inscripcionRepo.SaveChangesAsync();
    }
}
