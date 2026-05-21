using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IHorarioClaseRepository
{
    Task<IEnumerable<HorarioClase>> GetAllAsync(Guid? unidadId = null);
    Task<HorarioClase?> GetByIdAsync(Guid id);
    Task<IEnumerable<HorarioClase>> GetByClaseIdAsync(Guid claseId);
    Task<IEnumerable<HorarioClase>> GetByUnidadYDiaAsync(Guid unidadId, DiaSemana dia);
    Task AddAsync(HorarioClase horario);
    void Remove(HorarioClase horario);
    Task SaveChangesAsync();
}
