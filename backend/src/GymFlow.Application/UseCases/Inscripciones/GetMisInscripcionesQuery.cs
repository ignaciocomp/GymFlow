using System.Linq;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Inscripciones;

public class GetMisInscripcionesQuery
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;

    public GetMisInscripcionesQuery(IInscripcionClaseRepository inscripcionRepo)
    {
        _inscripcionRepo = inscripcionRepo;
    }

    public async Task<IEnumerable<InscripcionClaseDto>> ExecuteAsync(Guid socioId)
    {
        var inscripciones = (await _inscripcionRepo.GetBySocioIdAsync(socioId)).ToList();
        var claseIds = inscripciones.Select(i => i.ClaseId).Distinct();
        var conteos = await _inscripcionRepo.GetConteoActivasPorClasesAsync(claseIds);

        return inscripciones.Select(i =>
        {
            var ocupados = conteos.GetValueOrDefault(i.ClaseId, 0);
            // "Mis inscripciones" muestra solo el flag EnListaEspera, no la posición exacta
            // (calcularla por inscripción reintroduciría N+1). Por eso pasamos null.
            return InscripcionMapper.ToDto(i, i.Clase, ocupados, posicionListaEspera: null);
        });
    }
}
