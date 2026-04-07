namespace GymFlow.Application.DTOs;

public record UnidadDto(Guid Id, string Nombre, string Direccion, Guid? PlanId = null, string? PlanNombre = null);
