using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

public record ProcesarRecordatoriosResultado(int Enviados, int Omitidos, int Fallidos);

/// <summary>
/// RF-06: procesa los recordatorios automáticos del día para todas las cuotas pendientes
/// que vencen a 5 días, 1 día o hoy. Evita duplicados, registra resultado de cada envío.
/// Esta lógica se ejecuta diariamente desde el RecordatorioBackgroundService pero está
/// aquí (Application) para que sea testeable con mocks.
/// </summary>
public class ProcesarRecordatoriosCommand
{
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IRecordatorioCuotaRepository _recordatorioRepository;
    private readonly IEmailService _emailService;
    private readonly INotificadorInApp _notificador;

    public ProcesarRecordatoriosCommand(
        ICuotaRepository cuotaRepository,
        IRecordatorioCuotaRepository recordatorioRepository,
        IEmailService emailService,
        INotificadorInApp notificador)
    {
        _cuotaRepository = cuotaRepository;
        _recordatorioRepository = recordatorioRepository;
        _emailService = emailService;
        _notificador = notificador;
    }

    public async Task<ProcesarRecordatoriosResultado> ExecuteAsync(DateTime hoy)
    {
        var cuotas = await _cuotaRepository.GetCuotasParaRecordatorioAsync(hoy);

        var enviados = 0;
        var omitidos = 0;
        var fallidos = 0;
        var sociosNotificados = new List<Guid>();

        foreach (var cuota in cuotas)
        {
            var tipo = ResolverTipo(cuota.FechaVencimiento.Date, hoy.Date);
            if (tipo is null) continue;

            if (string.IsNullOrWhiteSpace(cuota.Socio.Correo))
            {
                await _recordatorioRepository.AddAsync(new RecordatorioCuota(
                    cuota.Id, cuota.SocioId, tipo.Value,
                    exitoso: false, error: "Socio sin correo registrado."));
                omitidos++;
                continue;
            }

            if (await _recordatorioRepository.ExisteRecordatorioHoyAsync(cuota.Id, tipo.Value))
            {
                omitidos++;
                continue;
            }

            var (asunto, cuerpo) = ConstruirEmail(tipo.Value, cuota);
            var resultado = await _emailService.EnviarAsync(cuota.Socio.Correo, asunto, cuerpo);

            await _recordatorioRepository.AddAsync(new RecordatorioCuota(
                cuota.Id, cuota.Socio.Id, tipo.Value,
                exitoso: resultado.Exitoso, error: resultado.Error));

            if (resultado.Exitoso)
            {
                enviados++;
                sociosNotificados.Add(cuota.SocioId);
            }
            else fallidos++;
        }

        await _recordatorioRepository.SaveChangesAsync();

        // Notificación in-app en batch, una sola vez, después del SaveChanges de los recordatorios.
        // Best-effort: si la creación falla, el job igual devuelve su resultado.
        if (sociosNotificados.Count > 0)
        {
            try
            {
                await _notificador.CrearParaVariosAsync(
                    sociosNotificados,
                    TipoNotificacion.RecordatorioCuota,
                    "Tenés una cuota pendiente",
                    "Te recordamos que tenés una cuota pendiente en GymFlow. Por favor regularizá tu pago a la brevedad.");
            }
            catch
            {
                // Best-effort: la creación de las notificaciones in-app nunca rompe el job.
            }
        }

        return new ProcesarRecordatoriosResultado(enviados, omitidos, fallidos);
    }

    /// <summary>
    /// Determina qué tipo de recordatorio corresponde según la diferencia de días.
    /// </summary>
    public static TipoRecordatorio? ResolverTipo(DateTime fechaVencimiento, DateTime hoy)
    {
        var diferencia = (fechaVencimiento - hoy).Days;
        return diferencia switch
        {
            5 => TipoRecordatorio.CincoDias,
            1 => TipoRecordatorio.UnDia,
            0 => TipoRecordatorio.DiaVencimiento,
            _ => null
        };
    }

    private static (string Asunto, string Cuerpo) ConstruirEmail(TipoRecordatorio tipo, Cuota cuota)
        => EmailTemplates.Automatico(tipo, cuota.Socio, cuota);
}
