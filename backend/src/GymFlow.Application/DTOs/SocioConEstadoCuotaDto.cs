namespace GymFlow.Application.DTOs;

public enum EstadoGeneralCuotas
{
    AlDia,        // Todas las cuotas pagas (o sin cuotas)
    Pendiente,    // Tiene cuotas pendientes pero ninguna vencida
    Vencido       // Tiene al menos una cuota pendiente y vencida
}

public record SocioConEstadoCuotaDto(
    Guid SocioId,
    string Nombre,
    string Apellido,
    string Correo,
    string? DocumentoIdentidad,
    IEnumerable<string> Unidades,
    EstadoGeneralCuotas Estado,
    int CuotasPendientes,
    int CuotasVencidas);
