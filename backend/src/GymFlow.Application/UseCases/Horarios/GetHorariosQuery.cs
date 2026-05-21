using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Horarios;

public class GetHorariosQuery
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IClaseRepository _claseRepo;

    public GetHorariosQuery(IHorarioClaseRepository horarioRepo, IClaseRepository claseRepo)
    {
        _horarioRepo = horarioRepo;
        _claseRepo = claseRepo;
    }

    public async Task<IEnumerable<HorarioClaseDto>> ExecuteAsync(Guid? unidadId = null)
    {
        var horarios = await _horarioRepo.GetAllAsync(unidadId);
        var result = new List<HorarioClaseDto>();

        foreach (var h in horarios)
        {
            var inscripciones = await _claseRepo.GetInscripcionesActivasCountAsync(h.ClaseId);
            result.Add(HorarioMapper.ToDto(h, inscripciones));
        }

        return result;
    }
}
