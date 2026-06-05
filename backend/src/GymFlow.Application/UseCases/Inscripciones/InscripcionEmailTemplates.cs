using System.Net;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Inscripciones;

internal static class InscripcionEmailTemplates
{
    public static (string Asunto, string Cuerpo) Confirmacion(Socio socio, Clase clase)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var asunto = $"Inscripción confirmada: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Tu inscripción a la clase <strong>{claseNombre}</strong> fue confirmada.</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }

    public static (string Asunto, string Cuerpo) CupoLiberado(Socio socio, Clase clase)
    {
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var claseNombre = WebUtility.HtmlEncode(clase.Nombre);
        var asunto = $"Se liberó un cupo: {claseNombre}";
        var cuerpo = $@"<html><body style='font-family:Arial,sans-serif;'>
<h2>Hola {nombre},</h2>
<p>Se liberó un cupo en <strong>{claseNombre}</strong> y ya quedaste inscripto.</p>
<p>Saludos,<br/>Equipo GymFlow</p></body></html>";
        return (asunto, cuerpo);
    }
}
