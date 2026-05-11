using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public class GetCuotasAdminQuery
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly ISocioRepository _socioRepository;

    public GetCuotasAdminQuery(ICuotaRepository cuotaRepository, ISocioRepository socioRepository)
    {
        _cuotaRepository = cuotaRepository;
        _socioRepository = socioRepository;
    }

    public async Task<IEnumerable<CuotaDto>> ExecuteAsync(string documentoIdentidad, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false)
    {
        var socio = (await _socioRepository.GetAllAsync(includeInactive: true))
            .FirstOrDefault(s => s.DocumentoIdentidad == documentoIdentidad)
            ?? throw new KeyNotFoundException("No se encontró un socio con ese documento de identidad.");

        var cuotas = await _cuotaRepository.SearchAsync(socio.Id, estado, mes, anio, unidadId, incluirAnuladas);
        return cuotas.Select(GetCuotasBySocioQuery.MapToDto);
    }
}
