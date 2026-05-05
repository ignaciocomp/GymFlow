using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Empleados;

public class GetEmpleadosQuery
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;

    public GetEmpleadosQuery(IEmpleadoRepository empleadoRepository, IRolRepository rolRepository)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
    }

    public async Task<IReadOnlyList<EmpleadoDto>> ExecuteAsync(bool? estaActivo = null, CancellationToken ct = default)
    {
        var empleados = await _empleadoRepository.GetAllAsync(estaActivo, ct);
        var roles = await _rolRepository.GetAllAsync(ct);
        var rolMap = roles.ToDictionary(r => r.Id, r => r.Nombre);

        return empleados.Select(e => new EmpleadoDto(
            e.Id, e.Nombre, e.Apellido, e.Correo,
            e.RolId,
            e.RolId.HasValue && rolMap.TryGetValue(e.RolId.Value, out var n) ? n : null,
            e.EstaActivo, e.FechaCreacion)).ToList();
    }
}
