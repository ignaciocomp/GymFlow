using GymFlow.Application.Interfaces;

namespace GymFlow.Application.UseCases.Cuotas;

public record GenerarCuotasResultado(int Generadas);

/// <summary>
/// Genera las cuotas del período para todos los socios activos que tengan un plan
/// asignado en alguna de sus unidades y cuya última cuota esté vencida (o no exista).
/// Es idempotente: si la última cuota sigue vigente no genera nada, por lo que correrlo
/// varias veces el mismo día no produce duplicados.
///
/// Esta lógica vive acá (Application) para que sea testeable con mocks y reutilizable
/// tanto desde CuotaGeneracionBackgroundService como desde el endpoint manual del controller.
/// </summary>
public class GenerarCuotasCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly ICuotaRepository _cuotaRepository;
    private readonly ICuotaGeneradorService _cuotaGenerador;

    public GenerarCuotasCommand(
        ISocioRepository socioRepository,
        ICuotaRepository cuotaRepository,
        ICuotaGeneradorService cuotaGenerador)
    {
        _socioRepository = socioRepository;
        _cuotaRepository = cuotaRepository;
        _cuotaGenerador = cuotaGenerador;
    }

    public async Task<GenerarCuotasResultado> ExecuteAsync()
    {
        var sociosActivos = await _socioRepository.GetAllAsync(includeInactive: false);

        var cuotasGeneradas = 0;

        foreach (var socio in sociosActivos)
        {
            foreach (var uu in socio.UnidadesAsignadas.Where(u => u.PlanId.HasValue))
            {
                var ultimaCuota = await _cuotaRepository.GetUltimaCuotaAsync(socio.Id, uu.UnidadId);

                if (ultimaCuota == null || ultimaCuota.FechaVencimiento <= DateTime.UtcNow)
                {
                    var fechaEmision = ultimaCuota?.FechaVencimiento ?? DateTime.UtcNow;
                    await _cuotaGenerador.GenerarCuotaAsync(socio.Id, uu, fechaEmision);
                    cuotasGeneradas++;
                }
            }
        }

        await _cuotaRepository.SaveChangesAsync();

        return new GenerarCuotasResultado(cuotasGeneradas);
    }
}
