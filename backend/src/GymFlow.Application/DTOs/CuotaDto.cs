using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record CuotaDto(
    Guid Id,
    string NombrePlan,
    string NombreUnidad,
    string? NombreSocio,
    decimal Monto,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    EstadoCuota Estado,
    DateTime? FechaPago,
    DateTime? FechaBaja);
