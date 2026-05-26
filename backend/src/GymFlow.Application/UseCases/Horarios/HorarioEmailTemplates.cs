using System.Net;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Horarios;

/// <summary>
/// Plantillas HTML para emails de cambio de horario.
/// Aplica HtmlEncode a todos los valores dinámicos.
/// </summary>
internal static class HorarioEmailTemplates
{
    public static (string Asunto, string Cuerpo) CambioHorario(
        Socio socio, Clase clase,
        DiaSemana diaAnterior, TimeOnly inicioAnterior, TimeOnly finAnterior,
        HorarioClase nuevoHorario)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var instructor = WebUtility.HtmlEncode(clase.Instructor);

        var asunto = $"Cambio de horario: {WebUtility.HtmlEncode(clase.Nombre)}";
        var cuerpo = $@"<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hola {nombre},</h2>
    <p>Te informamos que hubo un cambio en el horario de tu clase:</p>
    <table style='border-collapse: collapse;'>
        <tr><td><b>Clase:</b></td><td>{claseNombre}</td></tr>
        <tr><td><b>Instructor:</b></td><td>{instructor}</td></tr>
        <tr><td><b>Horario anterior:</b></td><td>{WebUtility.HtmlEncode(diaAnterior.ToString())} {inicioAnterior:HH:mm} - {finAnterior:HH:mm}</td></tr>
        <tr><td><b>Nuevo horario:</b></td><td>{WebUtility.HtmlEncode(nuevoHorario.DiaSemana.ToString())} {nuevoHorario.HoraInicio:HH:mm} - {nuevoHorario.HoraFin:HH:mm}</td></tr>
    </table>
    <p>Saludos,<br/>Equipo GymFlow</p>
</body>
</html>";

        return (asunto, cuerpo);
    }
}
