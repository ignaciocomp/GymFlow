using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Socios;

public class GetSociosQuery
{
    private readonly ISocioRepository _repository;

    public GetSociosQuery(ISocioRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SocioDto>> ExecuteAsync(
        string? nombre = null,
        Guid? unidadId = null,
        Guid? planId = null,
        bool? estaActivo = null)
    {
        var socios = await _repository.SearchAsync(nombre, unidadId, planId, estaActivo);

        return socios.Select(s => new SocioDto(
            Id: s.Id,
            Nombre: s.Nombre,
            Apellido: s.Apellido,
            Correo: s.Correo,
            Telefono: s.Telefono,
            TipoDocumento: s.TipoDocumento,
            DocumentoIdentidad: s.DocumentoIdentidad,
            FechaNacimiento: s.FechaNacimiento,
            FechaAlta: s.FechaAlta,
            EstaActivo: s.EstaActivo,
            PlanId: s.PlanId,
            PlanNombre: s.Plan?.Nombre,
            Unidades: s.UnidadesAsignadas
                .Select(uu => new UnidadDto(uu.UnidadId, uu.Unidad?.Nombre ?? "", uu.Unidad?.Direccion ?? ""))
                .ToList()));
    }
}
