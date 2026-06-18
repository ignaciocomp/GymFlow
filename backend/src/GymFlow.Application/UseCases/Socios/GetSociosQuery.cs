using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

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
        bool? estaActivo = null,
        IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        var socios = await _repository.SearchAsync(nombre, unidadId, planId, estaActivo, unidadesPermitidas);

        return socios.Select(s => MapToDto(s));
    }

    private static SocioDto MapToDto(Socio socio) => CreateSocioCommand.MapToDto(socio);
}
