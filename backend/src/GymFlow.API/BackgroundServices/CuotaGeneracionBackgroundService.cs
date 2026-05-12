using GymFlow.Application.Interfaces;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.API.BackgroundServices;

public class CuotaGeneracionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CuotaGeneracionBackgroundService> _logger;

    public CuotaGeneracionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CuotaGeneracionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var habilitado = _configuration.GetValue<bool>("CuotaGeneracion:Habilitado");
        if (!habilitado)
        {
            _logger.LogInformation("CuotaGeneracionBackgroundService está deshabilitado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalcularDelay();
            _logger.LogInformation("Próxima generación de cuotas en {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await GenerarCuotasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la generación automática de cuotas.");
            }
        }
    }

    private TimeSpan CalcularDelay()
    {
        var horaConfig = _configuration["CuotaGeneracion:HoraEjecucion"] ?? "03:00";
        var hora = TimeSpan.Parse(horaConfig);
        var ahora = DateTime.UtcNow;
        var proximaEjecucion = ahora.Date.Add(hora);

        if (proximaEjecucion <= ahora)
            proximaEjecucion = proximaEjecucion.AddDays(1);

        return proximaEjecucion - ahora;
    }

    private async Task GenerarCuotasAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymFlowDbContext>();
        var cuotaGenerador = scope.ServiceProvider.GetRequiredService<ICuotaGeneradorService>();
        var cuotaRepo = scope.ServiceProvider.GetRequiredService<ICuotaRepository>();

        var sociosActivos = await db.Socios
            .Include(s => s.UnidadesAsignadas)
            .Where(s => s.EstaActivo)
            .ToListAsync();

        var cuotasGeneradas = 0;

        foreach (var socio in sociosActivos)
        {
            foreach (var uu in socio.UnidadesAsignadas.Where(u => u.PlanId.HasValue))
            {
                var ultimaCuota = await cuotaRepo.GetUltimaCuotaAsync(socio.Id, uu.UnidadId);

                if (ultimaCuota == null || ultimaCuota.FechaVencimiento <= DateTime.UtcNow)
                {
                    var fechaEmision = ultimaCuota?.FechaVencimiento ?? DateTime.UtcNow;
                    await cuotaGenerador.GenerarCuotaAsync(socio.Id, uu, fechaEmision);
                    cuotasGeneradas++;
                }
            }
        }

        await cuotaRepo.SaveChangesAsync();
        _logger.LogInformation("Generación automática completada: {Count} cuotas generadas.", cuotasGeneradas);
    }
}
