namespace GymFlow.Application.DTOs;

public record ActualizarRolRequest(string Nombre, IReadOnlyList<Guid> PermisoIds);
