using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Eventos;

public class GetEventosPortalQuery
{
    private readonly IEventoRepository _eventoRepository;
    private readonly ISocioRepository _socioRepository;

    public GetEventosPortalQuery(IEventoRepository eventoRepository, ISocioRepository socioRepository)
    {
        _eventoRepository = eventoRepository;
        _socioRepository = socioRepository;
    }

    public async Task<IEnumerable<EventoDto>> ExecuteAsync(string correoSocio)
    {
        var socio = await _socioRepository.GetByCorreoAsync(correoSocio)
            ?? throw new KeyNotFoundException($"No se encontró el socio con correo {correoSocio}.");

        var unidadIds = socio.UnidadesAsignadas.Select(uu => uu.UnidadId).Distinct();

        var eventos = await _eventoRepository.GetProximosByUnidadesAsync(unidadIds, DateTime.UtcNow);
        return eventos.Select(EventoMapper.ToDto);
    }
}
