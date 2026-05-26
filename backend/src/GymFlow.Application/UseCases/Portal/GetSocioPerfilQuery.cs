using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Socios;

namespace GymFlow.Application.UseCases.Portal;

public class GetSocioPerfilQuery
{
    private readonly ISocioRepository _socioRepository;

    public GetSocioPerfilQuery(ISocioRepository socioRepository)
    {
        _socioRepository = socioRepository;
    }

    public async Task<SocioDto> ExecuteAsync(string correo)
    {
        var socio = await _socioRepository.GetByCorreoAsync(correo)
            ?? throw new KeyNotFoundException($"No se encontró el socio con correo {correo}.");

        return CreateSocioCommand.MapToDto(socio);
    }
}
