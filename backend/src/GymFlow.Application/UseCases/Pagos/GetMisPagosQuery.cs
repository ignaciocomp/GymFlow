using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Pagos;

/// <summary>
/// RF-21 / CU-08: historial de pagos del socio (fecha, monto, medio, N° de transacción MP,
/// estado y plan). El repositorio ya devuelve los pagos ordenados por fecha descendente
/// e incluye la Cuota (para el nombre del plan).
/// </summary>
public class GetMisPagosQuery
{
    private readonly IPagoRepository _pagoRepository;

    public GetMisPagosQuery(IPagoRepository pagoRepository) => _pagoRepository = pagoRepository;

    public async Task<IEnumerable<PagoDto>> ExecuteAsync(Guid socioId)
    {
        var pagos = await _pagoRepository.GetBySocioIdAsync(socioId);
        return pagos.Select(MapToDto).ToList();
    }

    private static PagoDto MapToDto(Pago p) => new(
        Id: p.Id,
        Fecha: p.FechaCreacion,
        Monto: p.Monto,
        MedioPago: p.MedioPago,
        MpPaymentId: p.MpPaymentId,
        Estado: p.Estado,
        NombrePlan: p.Cuota?.NombrePlan ?? string.Empty);
}
