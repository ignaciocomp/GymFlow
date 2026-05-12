using GymFlow.Application.UseCases.Cuotas;

namespace GymFlow.API.BackgroundServices;

/// <summary>
/// RF-06: corre una vez al día y delega el procesamiento a ProcesarRecordatoriosCommand.
/// Mantenemos esta clase mínima — la lógica de negocio está en Application
/// (ProcesarRecordatoriosCommand) para que sea testeable con mocks.
/// </summary>
public class RecordatorioBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecordatorioBackgroundService> _logger;

    public RecordatorioBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RecordatorioBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var habilitado = _configuration.GetValue<bool>("Recordatorios:Habilitado");
        if (!habilitado)
        {
            _logger.LogInformation("RecordatorioBackgroundService está deshabilitado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalcularDelay();
            _logger.LogInformation("Próximo envío de recordatorios en {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await ProcesarAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el envío automático de recordatorios.");
            }
        }
    }

    private TimeSpan CalcularDelay()
    {
        var horaConfig = _configuration["Recordatorios:HoraEjecucion"] ?? "08:00";
        var hora = TimeSpan.Parse(horaConfig);
        var ahora = DateTime.UtcNow;
        var proximaEjecucion = ahora.Date.Add(hora);

        if (proximaEjecucion <= ahora)
            proximaEjecucion = proximaEjecucion.AddDays(1);

        return proximaEjecucion - ahora;
    }

    private async Task ProcesarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var command = scope.ServiceProvider.GetRequiredService<ProcesarRecordatoriosCommand>();

        var resultado = await command.ExecuteAsync(DateTime.UtcNow);

        _logger.LogInformation(
            "Recordatorios automáticos: {Enviados} enviados, {Omitidos} omitidos, {Fallidos} fallidos.",
            resultado.Enviados, resultado.Omitidos, resultado.Fallidos);
    }
}
