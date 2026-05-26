namespace GymFlow.Application.DTOs;

public record CreatePlanRequest(string Nombre, Guid UnidadId, decimal Precio, string? Descripcion);
