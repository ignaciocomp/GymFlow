using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Roles;

public class GetRolByIdQuery
{
    private readonly IRolRepository _rolRepository;

    public GetRolByIdQuery(IRolRepository rolRepository) => _rolRepository = rolRepository;

    public async Task<RolDto> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var rol = await _rolRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rol {id} no encontrado.");

        return new RolDto(
            rol.Id, rol.Nombre, rol.EsSistema, rol.FechaCreacion,
            rol.Permisos.Select(rp => new PermisoDto(rp.Permiso.Id, rp.Permiso.Modulo, rp.Permiso.Operacion)).ToList()
        );
    }
}
