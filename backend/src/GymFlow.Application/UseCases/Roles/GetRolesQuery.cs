using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Roles;

public class GetRolesQuery
{
    private readonly IRolRepository _rolRepository;

    public GetRolesQuery(IRolRepository rolRepository) => _rolRepository = rolRepository;

    public async Task<IReadOnlyList<RolDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var roles = await _rolRepository.GetAllAsync(ct);
        return roles.Select(r => new RolDto(
            r.Id,
            r.Nombre,
            r.EsSistema,
            r.FechaCreacion,
            r.Permisos.Select(rp => new PermisoDto(rp.Permiso.Id, rp.Permiso.Modulo, rp.Permiso.Operacion)).ToList()
        )).ToList();
    }
}
