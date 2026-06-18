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

    public async Task<IEnumerable<CuotaDto>> ExecuteAsync(string documentoIdentidad, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false, IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        var socio = await _socioRepository.GetByDocumentoIdentidadAsync(documentoIdentidad)
            ?? throw new KeyNotFoundException("No se encontró un socio con ese documento de identidad.");

        if (!SocioVisible(socio, unidadesPermitidas))
            return [];

        var cuotas = await _cuotaRepository.SearchAsync(socio.Id, estado, mes, anio, unidadId, incluirAnuladas);
        return cuotas.Select(GetCuotasBySocioQuery.MapToDto);
    }

    public async Task<IEnumerable<CuotaDto>> ExecuteBySocioIdAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false, IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        var socio = await _socioRepository.GetByIdAsync(socioId)
            ?? throw new KeyNotFoundException("No se encontró el socio.");

        if (!SocioVisible(socio, unidadesPermitidas))
            return [];

        var cuotas = await _cuotaRepository.SearchAsync(socio.Id, estado, mes, anio, unidadId, incluirAnuladas);
        return cuotas.Select(GetCuotasBySocioQuery.MapToDto);
    }

    // El Dueño solo ve cuotas de socios que pertenezcan a alguna de sus unidades.
    // null = sin restricción (Admin u otro empleado).
    private static bool SocioVisible(Domain.Entities.Socio socio, IReadOnlyCollection<Guid>? unidadesPermitidas)
        => unidadesPermitidas is null
            || socio.UnidadesAsignadas.Any(uu => unidadesPermitidas.Contains(uu.UnidadId));
}
