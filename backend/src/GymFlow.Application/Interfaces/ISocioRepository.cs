using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface ISocioRepository
{
    Task<IEnumerable<Socio>> GetAllAsync(bool includeInactive = false);
    Task<Socio?> GetByIdAsync(Guid id);
    Task<Socio?> GetByDocumentoIdentidadAsync(string documentoIdentidad);
    Task<Socio?> GetByCorreoAsync(string correo);
    Task<bool> ExisteCorreoAsync(string correo);
    Task<bool> ExisteCedulaAsync(string cedula);
    Task<IEnumerable<Socio>> SearchAsync(string? nombre, Guid? unidadId, Guid? planId, bool? estaActivo, IReadOnlyCollection<Guid>? unidadesPermitidas = null);
    Task<IEnumerable<Socio>> GetActivosByUnidadAsync(Guid unidadId);
    Task<int> CountActivosByUnidadAsync(Guid unidadId);
    /// <summary>
    /// RF-18: total de socios activos (DISTINCT: asignado a varias unidades cuenta una vez).
    /// <paramref name="unidadIds"/> null = todas las unidades.
    /// </summary>
    Task<int> CountActivosAsync(IReadOnlyCollection<Guid>? unidadIds = null);
    Task AddAsync(Socio socio);
    Task SaveChangesAsync();
}
