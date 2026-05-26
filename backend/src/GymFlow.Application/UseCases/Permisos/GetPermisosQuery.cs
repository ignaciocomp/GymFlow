using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Permisos;

public class GetPermisosQuery
{
    private readonly IPermisoRepository _permisoRepository;

    public GetPermisosQuery(IPermisoRepository permisoRepository) => _permisoRepository = permisoRepository;

    public async Task<IReadOnlyList<PermisoDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var permisos = await _permisoRepository.GetAllAsync(ct);
        return permisos.Select(p => new PermisoDto(p.Id, p.Modulo, p.Operacion)).ToList();
    }
}
