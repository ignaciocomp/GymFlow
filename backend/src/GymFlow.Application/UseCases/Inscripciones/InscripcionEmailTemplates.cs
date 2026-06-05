using System.Net;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionEmailTemplates
{
    public static (string Asunto, string Cuerpo) Confirmacion(Socio socio, HorarioClase horario)
    {
        var clase = horario.Clase;
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var sala = WebUtility.HtmlEncode(horario.Sala ?? "Sin sala");
        var asunto = $"Inscripcion confirmada: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Tu inscripcion a la clase <strong>{claseNombre}</strong> fue confirmada.</p>
<p><strong>Horario:</strong> {horario.DiaSemana} {horario.HoraInicio:HH:mm} - {horario.HoraFin:HH:mm}<br/>
<strong>Sala:</strong> {sala}</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }

    public static (string Asunto, string Cuerpo) CupoLiberado(Socio socio, HorarioClase horario)
    {
        var clase = horario.Clase;
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var sala = WebUtility.HtmlEncode(horario.Sala ?? "Sin sala");
        var asunto = $"Se libero un cupo: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Se libero un cupo en <strong>{claseNombre}</strong> y ya quedaste inscripto.</p>
<p><strong>Horario:</strong> {horario.DiaSemana} {horario.HoraInicio:HH:mm} - {horario.HoraFin:HH:mm}<br/>
<strong>Sala:</strong> {sala}</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }
}
