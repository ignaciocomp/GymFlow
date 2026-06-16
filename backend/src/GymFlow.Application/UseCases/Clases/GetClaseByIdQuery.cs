using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Clases;

public class GetClaseByIdQuery
{
    private readonly IClaseRepository _repository;
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetClaseByIdQuery(IClaseRepository repository, IHorarioClaseRepository horarioRepo, IInscripcionClaseRepository inscripcionRepo)
    {
        _repository = repository;
        _horarioRepo = horarioRepo;
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<ClaseDto> ExecuteAsync(Guid id)
    {
        var clase = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("La clase no fue encontrada.");

        var horarioIds = (await _horarioRepo.GetByClaseIdAsync(id)).Select(h => h.Id).ToList();
        var conteos = horarioIds.Count > 0
            ? await _inscripcionRepo.GetConteoActivasPorHorariosAsync(horarioIds)
            : new Dictionary<Guid, int>();
        var total = horarioIds.Sum(hId => conteos.GetValueOrDefault(hId, 0));

        return new ClaseDto(clase.Id, clase.Nombre, clase.Descripcion, clase.CapacidadMaxima, clase.DuracionMinutos,
            clase.Instructor, clase.UnidadId, clase.Unidad?.Nombre ?? "", clase.EstaActivo, total);
    }
}
