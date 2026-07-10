using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Pagos;

/// <summary>
/// Fila del historial de pagos del socio (RF-21 / CU-08).
/// </summary>
public record PagoDto(
    Guid Id,
    DateTime Fecha,
    decimal Monto,
    string? MedioPago,
    string? MpPaymentId,
    EstadoPago Estado,
    string NombrePlan);
