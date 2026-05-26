using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Socios;

public class GetSocioByIdQuery
{
    private readonly ISocioRepository _socioRepository;

    public GetSocioByIdQuery(ISocioRepository socioRepository)
    {
        _socioRepository = socioRepository;
    }

    public async Task<SocioDto> ExecuteAsync(Guid id)
    {
        var socio = await _socioRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró el socio con ID {id}.");

        return MapToDto(socio);
    }

    private static SocioDto MapToDto(Socio socio) => CreateSocioCommand.MapToDto(socio);
}
