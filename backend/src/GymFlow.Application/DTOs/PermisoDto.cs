using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

public record PermisoDto(Guid Id, Modulo Modulo, Operacion Operacion);
