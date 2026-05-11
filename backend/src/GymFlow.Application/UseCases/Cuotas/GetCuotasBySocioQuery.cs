using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Cuotas;

public class GetCuotasBySocioQuery
{
    private readonly ICuotaRepository _cuotaRepository;

    public GetCuotasBySocioQuery(ICuotaRepository cuotaRepository) => _cuotaRepository = cuotaRepository;

    public async Task<IEnumerable<CuotaDto>> ExecuteAsync(Guid socioId)
    {
        var cuotas = await _cuotaRepository.GetBySocioIdAsync(socioId);
        return cuotas.Select(MapToDto);
    }

    internal static CuotaDto MapToDto(Cuota c) => new(
        Id: c.Id,
        NombrePlan: c.NombrePlan,
        NombreUnidad: c.Unidad?.Nombre ?? "",
        NombreSocio: c.Socio != null ? $"{c.Socio.Nombre} {c.Socio.Apellido}" : null,
        Monto: c.Monto,
        FechaEmision: c.FechaEmision,
        FechaVencimiento: c.FechaVencimiento,
        Estado: c.Estado,
        FechaPago: c.FechaPago,
        FechaBaja: c.FechaBaja);
}
