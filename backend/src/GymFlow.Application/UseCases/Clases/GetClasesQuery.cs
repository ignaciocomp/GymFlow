using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Clases;

public class GetClasesQuery
{
    private readonly IClaseRepository _repository;

    public GetClasesQuery(IClaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ClaseDto>> ExecuteAsync(Guid? unidadId = null, bool includeInactive = false)
    {
        var clases = unidadId.HasValue
            ? await _repository.GetByUnidadIdAsync(unidadId.Value, includeInactive)
            : await _repository.GetAllAsync(includeInactive);

        return clases.Select(c => new ClaseDto(c.Id, c.Nombre, c.Descripcion, c.CapacidadMaxima, c.DuracionMinutos,
            c.Instructor, c.UnidadId, c.Unidad?.Nombre ?? "", c.EstaActivo));
    }
}
