using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Horarios;

public class GetHorarioByIdQuery
{
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetHorarioByIdQuery(IHorarioClaseRepository horarioRepo, IInscripcionClaseRepository inscripcionRepo)
    {
        _horarioRepo = horarioRepo;
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<HorarioClaseDto> ExecuteAsync(Guid id)
    {
        var horario = await _horarioRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("El horario no fue encontrado.");

        var inscripciones = await _inscripcionRepo.GetInscripcionesActivasCountAsync(horario.Id);
        return HorarioMapper.ToDto(horario, inscripciones);
    }
}
