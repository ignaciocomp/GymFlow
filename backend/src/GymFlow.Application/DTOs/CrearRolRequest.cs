namespace GymFlow.Application.DTOs;

public record CrearRolRequest(string Nombre, IReadOnlyList<Guid> PermisoIds);
