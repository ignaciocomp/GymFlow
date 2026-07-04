using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IHorarioClaseRepository
{
    Task<IEnumerable<HorarioClase>> GetAllAsync(Guid? unidadId = null, IReadOnlyCollection<Guid>? unidadesPermitidas = null);
    Task<HorarioClase?> GetByIdAsync(Guid id);
    Task<IEnumerable<HorarioClase>> GetByClaseIdAsync(Guid claseId);
    Task<IEnumerable<HorarioClase>> GetByUnidadYDiaAsync(Guid unidadId, DiaSemana dia);
    /// <summary>
    /// RF-18: horarios de un día de la semana (solo clases activas, con Clase y Unidad),
    /// ordenados por hora de inicio. <paramref name="unidadIds"/> null = todas las unidades.
    /// </summary>
    Task<IEnumerable<HorarioClase>> GetByDiaAsync(DiaSemana dia, IReadOnlyCollection<Guid>? unidadIds = null);
    Task AddAsync(HorarioClase horario);
    void Remove(HorarioClase horario);
    Task SaveChangesAsync();
}
