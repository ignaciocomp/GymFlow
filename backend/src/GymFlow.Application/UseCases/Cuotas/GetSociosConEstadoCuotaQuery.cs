using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

/// <summary>
/// RF-07: lista todos los socios activos con un resumen del estado de sus cuotas
/// (Al día / Pendiente / Vencido). El admin usa esta vista para identificar rápidamente
/// quiénes tienen pagos pendientes antes de profundizar en el detalle de cuotas.
///
/// Implementación: hace 2 queries totales (socios + cuotas pendientes), agrupa las cuotas
/// por socioId en memoria, y calcula el estado por cada socio sin tocar la DB de nuevo.
/// Evita el patrón N+1.
/// </summary>
public class GetSociosConEstadoCuotaQuery
{
    private readonly ISocioRepository _socioRepository;
    private readonly ICuotaRepository _cuotaRepository;

    public GetSociosConEstadoCuotaQuery(ISocioRepository socioRepository, ICuotaRepository cuotaRepository)
    {
        _socioRepository = socioRepository;
        _cuotaRepository = cuotaRepository;
    }

    public async Task<IEnumerable<SocioConEstadoCuotaDto>> ExecuteAsync(Guid? unidadId = null)
    {
        // Query 1: traer todos los socios activos
        var socios = await _socioRepository.GetAllAsync(includeInactive: false);

        // Query 2: traer todas las cuotas pendientes (no anuladas) en una sola query.
        // Si hay filtro de unidad, la query las filtra del lado del SQL.
        var cuotasPendientes = await _cuotaRepository.GetCuotasPendientesDeTodosLosSociosAsync(unidadId);

        // Agrupar en memoria por socioId — O(1) lookup en el loop
        var cuotasPorSocio = cuotasPendientes
            .GroupBy(c => c.SocioId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var hoy = DateTime.UtcNow.Date;
        var resultado = new List<SocioConEstadoCuotaDto>();

        foreach (var socio in socios)
        {
            // Si hay filtro de unidad, saltear socios que no pertenezcan a esa unidad
            if (unidadId.HasValue && !socio.UnidadesAsignadas.Any(uu => uu.UnidadId == unidadId.Value))
                continue;

            var cuotasDelSocio = cuotasPorSocio.TryGetValue(socio.Id, out var lista)
                ? lista
                : new List<Domain.Entities.Cuota>();

            var pendientes = cuotasDelSocio.Count;
            var vencidas = cuotasDelSocio.Count(c => c.FechaVencimiento.Date < hoy);

            var estado = vencidas > 0
                ? EstadoGeneralCuotas.Vencido
                : pendientes > 0
                    ? EstadoGeneralCuotas.Pendiente
                    : EstadoGeneralCuotas.AlDia;

            var nombresUnidades = socio.UnidadesAsignadas
                .Where(uu => !unidadId.HasValue || uu.UnidadId == unidadId.Value)
                .Select(uu => uu.Unidad?.Nombre ?? "")
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();

            resultado.Add(new SocioConEstadoCuotaDto(
                SocioId: socio.Id,
                Nombre: socio.Nombre,
                Apellido: socio.Apellido,
                Correo: socio.Correo,
                DocumentoIdentidad: socio.DocumentoIdentidad,
                Unidades: nombresUnidades,
                Estado: estado,
                CuotasPendientes: pendientes,
                CuotasVencidas: vencidas));
        }

        // Ordenar: vencidos primero, después pendientes, después al día
        return resultado
            .OrderBy(s => s.Estado switch
            {
                EstadoGeneralCuotas.Vencido => 0,
                EstadoGeneralCuotas.Pendiente => 1,
                _ => 2
            })
            .ThenBy(s => s.Apellido)
            .ThenBy(s => s.Nombre);
    }
}
