using System.Net;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Clases;

/// <summary>
/// Plantillas HTML para emails relacionados con clases.
/// Aplica HtmlEncode a todos los valores dinámicos para prevenir inyección HTML.
/// </summary>
internal static class ClaseEmailTemplates
{
    public static (string Asunto, string Cuerpo) Cancelacion(Socio socio, Clase clase)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var instructor = WebUtility.HtmlEncode(clase.Instructor);
        var unidad = WebUtility.HtmlEncode(clase.Unidad?.Nombre ?? "");

        var asunto = $"Clase cancelada: {WebUtility.HtmlEncode(clase.Nombre)}";
        var cuerpo = $@"<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hola {nombre},</h2>
    <p>Te informamos que la siguiente clase ha sido cancelada:</p>
    <table style='border-collapse: collapse;'>
        <tr><td><b>Clase:</b></td><td>{claseNombre}</td></tr>
        <tr><td><b>Instructor:</b></td><td>{instructor}</td></tr>
        <tr><td><b>Sede:</b></td><td>{unidad}</td></tr>
    </table>
    <p>Disculpá las molestias.</p>
    <p>Saludos,<br/>Equipo GymFlow</p>
</body>
</html>";

        return (asunto, cuerpo);
    }
}
