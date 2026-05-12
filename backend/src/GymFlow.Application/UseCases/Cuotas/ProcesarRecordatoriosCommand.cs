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

    public ProcesarRecordatoriosCommand(
        ICuotaRepository cuotaRepository,
        IRecordatorioCuotaRepository recordatorioRepository,
        IEmailService emailService)
    {
        _cuotaRepository = cuotaRepository;
        _recordatorioRepository = recordatorioRepository;
        _emailService = emailService;
    }

    public async Task<ProcesarRecordatoriosResultado> ExecuteAsync(DateTime hoy)
    {
        var cuotas = await _cuotaRepository.GetCuotasParaRecordatorioAsync(hoy);

        var enviados = 0;
        var omitidos = 0;
        var fallidos = 0;

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

            if (resultado.Exitoso) enviados++;
            else fallidos++;
        }

        await _recordatorioRepository.SaveChangesAsync();

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
