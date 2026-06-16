using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Horarios;

public class GetHorariosQuery
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetHorariosQuery(IHorarioClaseRepository horarioRepo, IInscripcionClaseRepository inscripcionRepo)
    {
        _horarioRepo = horarioRepo;
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<IEnumerable<HorarioClaseDto>> ExecuteAsync(Guid? unidadId = null)
    {
        var horarios = (await _horarioRepo.GetAllAsync(unidadId)).ToList();
        var horarioIds = horarios.Select(h => h.Id);
        var conteos = await _inscripcionRepo.GetConteoActivasPorHorariosAsync(horarioIds);
        return horarios.Select(h => HorarioMapper.ToDto(h, conteos.GetValueOrDefault(h.Id, 0)));
    }
}
