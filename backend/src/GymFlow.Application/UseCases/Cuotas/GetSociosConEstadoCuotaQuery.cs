using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

/// <summary>
/// RF-07: lista todos los socios activos con un resumen del estado de sus cuotas
/// (Al día / Pendiente / Vencido). El admin usa esta vista para identificar rápidamente
/// quiénes tienen pagos pendientes antes de profundizar en el detalle de cuotas.
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
        var socios = await _socioRepository.GetAllAsync(includeInactive: false);
        var hoy = DateTime.UtcNow.Date;
        var resultado = new List<SocioConEstadoCuotaDto>();

        foreach (var socio in socios)
        {
            // Si hay filtro de unidad, sacar los socios que no pertenezcan a esa unidad
            if (unidadId.HasValue && !socio.UnidadesAsignadas.Any(uu => uu.UnidadId == unidadId.Value))
                continue;

            var cuotas = await _cuotaRepository.SearchAsync(socio.Id, null, null, null, null, incluirAnuladas: false);

            // Si filtramos por unidad, calculamos el estado solo con sus cuotas de esa unidad
            var cuotasParaEstado = unidadId.HasValue
                ? cuotas.Where(c => c.UnidadId == unidadId.Value).ToList()
                : cuotas.ToList();

            var pendientes = cuotasParaEstado.Count(c => c.Estado == EstadoCuota.Pendiente);
            var vencidas = cuotasParaEstado.Count(c =>
                c.Estado == EstadoCuota.Pendiente && c.FechaVencimiento.Date < hoy);

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

        // Ordenar: vencidos primero, después pendientes, después al día — y dentro de cada estado por apellido
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
