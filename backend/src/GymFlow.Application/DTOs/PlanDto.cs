namespace GymFlow.Application.DTOs;

public record PlanDto(Guid Id, string Nombre, decimal Precio, string Descripcion, Guid UnidadId, string UnidadNombre, bool EstaActivo);
