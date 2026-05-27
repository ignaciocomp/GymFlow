using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Inscripciones;

public class GetMisInscripcionesQuery
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetMisInscripcionesQuery(IInscripcionClaseRepository inscripcionRepo)
    {
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<IEnumerable<InscripcionClaseDto>> ExecuteAsync(Guid socioId)
    {
        var inscripciones = await _inscripcionRepo.GetBySocioIdAsync(socioId);
        var result = new List<InscripcionClaseDto>();

        foreach (var i in inscripciones)
        {
            var count = await _inscripcionRepo.GetInscripcionesActivasCountAsync(i.ClaseId);
            result.Add(InscripcionMapper.ToDto(i, i.Clase, count));
        }

        return result;
    }
}
