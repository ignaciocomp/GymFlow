using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Empleados;

public class GetEmpleadoByIdQuery
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IRolRepository _rolRepository;

    public GetEmpleadoByIdQuery(IEmpleadoRepository empleadoRepository, IRolRepository rolRepository)
    {
        _empleadoRepository = empleadoRepository;
        _rolRepository = rolRepository;
    }

    public async Task<EmpleadoDto> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Empleado {id} no encontrado.");
        var rol = empleado.RolId.HasValue ? await _rolRepository.GetByIdAsync(empleado.RolId.Value, ct) : null;

        return new EmpleadoDto(
            empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Correo,
            empleado.RolId, rol?.Nombre,
            empleado.EstaActivo, empleado.FechaCreacion);
    }
}
