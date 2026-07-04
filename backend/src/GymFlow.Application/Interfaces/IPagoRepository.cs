using GymFlow.Domain.Entities;

namespace GymFlow.Application.Interfaces;

public interface IPagoRepository
{
    Task<Pago?> GetByIdAsync(Guid id);
    /// <summary>
    /// Devuelve el Pago cuyo Id coincide con el external_reference de la preferencia de Mercado Pago.
    /// El external_reference ES el Pago.Id, por lo que equivale a GetByIdAsync.
    /// </summary>
    Task<Pago?> GetByExternalReferenceAsync(Guid pagoId);
    Task<IEnumerable<Pago>> GetByCuotaIdAsync(Guid cuotaId);
    /// <summary>
    /// Historial de pagos del socio, ordenado por fecha de creación descendente, incluyendo la Cuota.
    /// </summary>
    Task<IEnumerable<Pago>> GetBySocioIdAsync(Guid socioId);
    Task AddAsync(Pago pago);
    Task SaveChangesAsync();
}
