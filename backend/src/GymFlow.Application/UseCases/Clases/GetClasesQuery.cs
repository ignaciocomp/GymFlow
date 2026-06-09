using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Clases;

public class GetClasesQuery
{
    private readonly IClaseRepository _repository;
    private readonly IHorarioClaseRepository _horarioRepo;
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetClasesQuery(IClaseRepository repository, IHorarioClaseRepository horarioRepo, IInscripcionClaseRepository inscripcionRepo)
    {
        _repository = repository;
        _horarioRepo = horarioRepo;
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<IEnumerable<ClaseDto>> ExecuteAsync(Guid? unidadId = null, bool includeInactive = false)
    {
        var clases = (unidadId.HasValue
            ? await _repository.GetByUnidadIdAsync(unidadId.Value, includeInactive)
            : await _repository.GetAllAsync(includeInactive)).ToList();

        var horariosPorClase = clases.Count > 0
            ? await _horarioRepo.GetByClaseIdsAsync(clases.Select(c => c.Id))
            : new Dictionary<Guid, List<HorarioClase>>();

        var claseHorarioIds = horariosPorClase.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(h => h.Id).ToList());
        var allHorarios = claseHorarioIds.Values.SelectMany(ids => ids).ToList();

        var conteos = allHorarios.Count > 0
            ? await _inscripcionRepo.GetConteoActivasPorHorariosAsync(allHorarios)
            : new Dictionary<Guid, int>();

        return clases.Select(c =>
        {
            var total = claseHorarioIds.GetValueOrDefault(c.Id, []).Sum(hId => conteos.GetValueOrDefault(hId, 0));
            return new ClaseDto(c.Id, c.Nombre, c.Descripcion, c.CapacidadMaxima, c.DuracionMinutos,
                c.Instructor, c.UnidadId, c.Unidad?.Nombre ?? "", c.EstaActivo, total);
        });
    }
}
