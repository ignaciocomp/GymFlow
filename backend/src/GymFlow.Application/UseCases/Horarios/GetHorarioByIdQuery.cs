using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Horarios;

public class GetHorarioByIdQuery
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IClaseRepository _claseRepo;

    public GetHorarioByIdQuery(IHorarioClaseRepository horarioRepo, IClaseRepository claseRepo)
    {
        _horarioRepo = horarioRepo;
        _claseRepo = claseRepo;
    }

    public async Task<HorarioClaseDto> ExecuteAsync(Guid id)
    {
        var horario = await _horarioRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El horario no fue encontrado.");

        var inscripciones = await _claseRepo.GetInscripcionesActivasCountAsync(horario.ClaseId);
        return HorarioMapper.ToDto(horario, inscripciones);
    }
}
