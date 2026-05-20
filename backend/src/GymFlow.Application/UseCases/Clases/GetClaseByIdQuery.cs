using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Clases;

public class GetClaseByIdQuery
{
    private readonly IClaseRepository _repository;

    public GetClaseByIdQuery(IClaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClaseDto> ExecuteAsync(Guid id)
    {
        var clase = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("La clase no fue encontrada.");

        var inscripcionesActivas = await _repository.GetInscripcionesActivasCountAsync(clase.Id);

        return new ClaseDto(clase.Id, clase.Nombre, clase.Descripcion, clase.CapacidadMaxima, clase.DuracionMinutos,
            clase.Instructor, clase.UnidadId, clase.Unidad?.Nombre ?? "", clase.EstaActivo, inscripcionesActivas);
    }
}
