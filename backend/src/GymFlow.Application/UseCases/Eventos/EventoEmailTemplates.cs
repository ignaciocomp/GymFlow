using System.Net;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Eventos;

/// <summary>
/// Plantillas HTML para emails relacionados con eventos.
/// Aplica HtmlEncode a todos los valores dinámicos para prevenir inyección HTML.
/// </summary>
internal static class EventoEmailTemplates
{
    public static (string Asunto, string Cuerpo) Notificacion(Socio socio, Evento evento)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var titulo = WebUtility.HtmlEncode(evento.Titulo);
        var descripcion = WebUtility.HtmlEncode(evento.Descripcion);
        var sede = WebUtility.HtmlEncode(evento.Unidad?.Nombre ?? "");
        var fecha = WebUtility.HtmlEncode(evento.Fecha.ToString("dd/MM/yyyy HH:mm"));

        var asunto = $"Nuevo evento en {sede}: {titulo}";
        var cuerpo = $@"<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hola {nombre},</h2>
    <p>Te invitamos al siguiente evento en tu sede:</p>
    <table style='border-collapse: collapse;'>
        <tr><td><b>Evento:</b></td><td>{titulo}</td></tr>
        <tr><td><b>Descripción:</b></td><td>{descripcion}</td></tr>
        <tr><td><b>Fecha:</b></td><td>{fecha}</td></tr>
        <tr><td><b>Sede:</b></td><td>{sede}</td></tr>
    </table>
    <p>¡Te esperamos!</p>
    <p>Saludos,<br/>Equipo GymFlow</p>
</body>
</html>";

        return (asunto, cuerpo);
    }
}
