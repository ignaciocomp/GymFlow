using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IInscripcionClaseRepository
{
    Task<InscripcionClase?> GetByIdAsync(Guid id);
    Task<IEnumerable<InscripcionClase>> GetBySocioIdAsync(Guid socioId);
    Task<InscripcionClase?> GetActivaBySocioYHorarioAsync(Guid socioId, Guid horarioClaseId);
    Task<int> GetInscripcionesActivasCountAsync(Guid horarioClaseId);
    Task<Dictionary<Guid, int>> GetConteoActivasPorHorariosAsync(IEnumerable<Guid> horarioClaseIds);
    Task<IEnumerable<InscripcionClase>> GetActivasByHorarioClaseIdAsync(Guid horarioClaseId);
    /// <summary>
    /// RF-18: últimas <paramref name="cantidad"/> inscripciones activas (con Socio, Clase y
    /// Unidad), más recientes primero. <paramref name="unidadIds"/> null = todas las unidades.
    /// </summary>
    Task<IEnumerable<InscripcionClase>> GetRecientesAsync(int cantidad, IReadOnlyCollection<Guid>? unidadIds = null);
    /// <summary>
    /// RF-18: conteo de inscripciones activas por día (fecha de inscripción) dentro de
    /// [desde, hasta]; los días sin inscripciones no aparecen en el diccionario.
    /// </summary>
    Task<Dictionary<DateTime, int>> GetConteoActivasPorDiaAsync(DateTime desde, DateTime hasta, IReadOnlyCollection<Guid>? unidadIds = null);
    Task AddAsync(InscripcionClase inscripcion);
    Task SaveChangesAsync();
}
