using GymFlow.Application.UseCases.Cuotas;

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
        var command = scope.ServiceProvider.GetRequiredService<GenerarCuotasCommand>();

        var resultado = await command.ExecuteAsync();

        _logger.LogInformation("Generación automática completada: {Count} cuotas generadas.", resultado.Generadas);
    }
}
