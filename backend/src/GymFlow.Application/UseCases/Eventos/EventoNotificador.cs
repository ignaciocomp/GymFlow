using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Eventos;

/// <summary>
/// Helper compartido de envío de la notificación de un evento por email.
/// Replica el patrón best-effort de <c>CancelClaseCommand</c>: envía en paralelo
/// (<see cref="Task.WhenAll(System.Threading.Tasks.Task[])"/>) a los socios activos
/// y cuenta los <see cref="EmailResultado.Exitoso"/> para el detalle de auditoría.
/// <see cref="IEmailService.EnviarAsync"/> no lanza (best-effort vía el flag).
/// </summary>
internal static class EventoNotificador
{
    internal record ResultadoNotificacion(int Total, int Enviados, int Fallidos);

    public static async Task<ResultadoNotificacion> NotificarAsync(
        IEmailService emailService, IEnumerable<Socio> socios, Evento evento, string sedeNombre)
    {
        var emailTasks = socios.Select(socio =>
        {
            var (asunto, cuerpo) = EventoEmailTemplates.Notificacion(socio, evento, sedeNombre);
            return emailService.EnviarAsync(socio.Correo, asunto, cuerpo);
        });

        var resultados = await Task.WhenAll(emailTasks);
        var enviados = resultados.Count(r => r.Exitoso);
        var fallidos = resultados.Length - enviados;

        return new ResultadoNotificacion(resultados.Length, enviados, fallidos);
    }
}
