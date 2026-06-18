using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

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

    public async Task<IEnumerable<ClaseDto>> ExecuteAsync(Guid? unidadId = null, bool includeInactive = false, IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        var clases = (unidadId.HasValue
            ? await _repository.GetByUnidadIdAsync(unidadId.Value, includeInactive, unidadesPermitidas)
            : await _repository.GetAllAsync(includeInactive, unidadesPermitidas)).ToList();

        var allHorarios = new List<Guid>();
        var claseHorarioIds = new Dictionary<Guid, List<Guid>>();
        foreach (var c in clases)
        {
            var horarios = (await _horarioRepo.GetByClaseIdAsync(c.Id)).Select(h => h.Id).ToList();
            claseHorarioIds[c.Id] = horarios;
            allHorarios.AddRange(horarios);
        }

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
