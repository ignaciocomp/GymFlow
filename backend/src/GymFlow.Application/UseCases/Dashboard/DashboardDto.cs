namespace GymFlow.Application.UseCases.Dashboard;

/// <summary>
/// Snapshot del dashboard operativo (RF-18 / CU-10). Se sirve tanto por el endpoint de
/// snapshot como por el stream SSE; MVC lo serializa en camelCase.
/// </summary>
public record DashboardDto(
    DateTime GeneradoEn,
    IReadOnlyList<DashboardUnidadDto> Unidades,
    SociosActivosDto SociosActivos,
    CuotasResumenDto Cuotas,
    IReadOnlyList<ClaseDelDiaDto> ClasesDelDia,
    IReadOnlyList<InscripcionRecienteDto> InscripcionesRecientes,
    GraficaDto Grafica);

/// <summary>Unidad visible para el filtro del dashboard.</summary>
public record DashboardUnidadDto(Guid Id, string Nombre);

public record SociosActivosDto(int Total, IReadOnlyList<SociosPorUnidadDto> PorUnidad);

public record SociosPorUnidadDto(Guid UnidadId, string UnidadNombre, int Cantidad);

/// <summary>Counts de cuotas calculados en vivo (RN-17).</summary>
public record CuotasResumenDto(int ProximasAVencer, int Vencidas, int PagadasMes);

/// <summary>Horario de clase programado para el día actual, con horas en formato HH:mm.</summary>
public record ClaseDelDiaDto(string Clase, string Unidad, string HoraInicio, string HoraFin, int Cupo, int Inscriptos);

public record InscripcionRecienteDto(string Socio, string Clase, string Unidad, DateTime Fecha);

public record GraficaDto(
    IReadOnlyList<SedeCantidadDto> SociosPorSede,
    IReadOnlyList<EstadoCantidadDto> CuotasPorEstado,
    IReadOnlyList<FechaCantidadDto> InscripcionesUltimos7Dias);

public record SedeCantidadDto(string Sede, int Cantidad);

public record EstadoCantidadDto(string Estado, int Cantidad);

/// <summary>Punto de la serie diaria; Fecha en formato yyyy-MM-dd (UTC).</summary>
public record FechaCantidadDto(string Fecha, int Cantidad);
