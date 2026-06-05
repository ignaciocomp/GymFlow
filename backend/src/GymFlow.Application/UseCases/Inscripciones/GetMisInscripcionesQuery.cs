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
        var inscripciones = (await _inscripcionRepo.GetBySocioIdAsync(socioId)).ToList();
        var horarioIds = inscripciones.Select(i => i.HorarioClaseId).Distinct();
        var conteos = await _inscripcionRepo.GetConteoActivasPorHorariosAsync(horarioIds);

        return inscripciones.Select(i =>
        {
            var ocupados = conteos.GetValueOrDefault(i.HorarioClaseId, 0);
            return InscripcionMapper.ToDto(i, ocupados);
        });
    }
}
