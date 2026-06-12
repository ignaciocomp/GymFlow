using System.Net;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Cuotas;

/// <summary>
/// Plantillas HTML compartidas para emails de cuotas (recordatorios y confirmación de pago).
/// Aplica HtmlEncode a todos los valores dinámicos para prevenir inyección HTML
/// (ej: un plan llamado "&lt;script&gt;..." no ejecuta código en el cliente de email).
/// </summary>
internal static class EmailTemplates
{
    public static (string Asunto, string Cuerpo) Manual(Socio socio, Cuota cuota)
    {
        var asunto = $"Recordatorio: tu cuota de {WebUtility.HtmlEncode(cuota.NombrePlan)} está pendiente";
        var encabezado = "Te recordamos que tenés una cuota pendiente en GymFlow.";
        var llamada = "Por favor regularizá tu pago a la brevedad.";
        return (asunto, BuildBody(socio, cuota, encabezado, llamada));
    }

    public static (string Asunto, string Cuerpo) Automatico(TipoRecordatorio tipo, Socio socio, Cuota cuota)
    {
        var (asunto, encabezado) = tipo switch
        {
            TipoRecordatorio.CincoDias => ("Tu cuota vence pronto", "Te recordamos que tu cuota vence en 5 días."),
            TipoRecordatorio.UnDia => ("Tu cuota vence mañana", "Tu cuota vence mañana — no olvides regularizar tu pago."),
            TipoRecordatorio.DiaVencimiento => ("Tu cuota vence hoy", "Tu cuota vence hoy. Por favor regularizá tu pago a la brevedad."),
            _ => ("Recordatorio de cuota", "Tenés una cuota pendiente.")
        };
        return (asunto, BuildBody(socio, cuota, encabezado, llamada: null));
    }

    public static (string Asunto, string Cuerpo) ConfirmacionPago(Socio socio, Cuota cuota)
    {
        // HtmlEncode en TODOS los valores dinámicos (mismo criterio que BuildBody).
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var plan = WebUtility.HtmlEncode(cuota.NombrePlan);
        var monto = cuota.Monto.ToString("N2");
        var periodo = cuota.FechaEmision.ToString("MM/yyyy");
        var fechaPago = (cuota.FechaPago ?? DateTime.UtcNow).ToString("dd/MM/yyyy");
        var filaSede = cuota.Unidad is null
            ? ""
            : $"<tr><td><b>Sede:</b></td><td>{WebUtility.HtmlEncode(cuota.Unidad.Nombre)}</td></tr>";

        var asunto = $"Pago confirmado: tu cuota de {plan}";
        var cuerpo = $@"<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hola {nombre},</h2>
    <p>Registramos el pago de tu cuota. ¡Gracias!</p>
    <table style='border-collapse: collapse;'>
        <tr><td><b>Plan:</b></td><td>{plan}</td></tr>
        {filaSede}
        <tr><td><b>Monto:</b></td><td>${monto}</td></tr>
        <tr><td><b>Período:</b></td><td>{periodo}</td></tr>
        <tr><td><b>Fecha de pago:</b></td><td>{fechaPago}</td></tr>
    </table>
    <p>Saludos,<br/>Equipo GymFlow</p>
</body>
</html>";
        return (asunto, cuerpo);
    }

    private static string BuildBody(Socio socio, Cuota cuota, string encabezado, string? llamada)
    {
        // HtmlEncode en TODOS los valores dinámicos. Defensa en profundidad
        // contra inyección HTML si alguien crea un plan/unidad/socio con HTML en su nombre.
        var nombre = WebUtility.HtmlEncode(socio.Nombre);
        var plan = WebUtility.HtmlEncode(cuota.NombrePlan);
        var unidad = WebUtility.HtmlEncode(cuota.Unidad?.Nombre ?? "");
        var monto = cuota.Monto.ToString("N2");
        var venc = cuota.FechaVencimiento.ToString("dd/MM/yyyy");
        var encabezadoSafe = WebUtility.HtmlEncode(encabezado);
        var llamadaSafe = llamada is null ? "" : $"<p>{WebUtility.HtmlEncode(llamada)}</p>";

        return $@"<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hola {nombre},</h2>
    <p>{encabezadoSafe}</p>
    <table style='border-collapse: collapse;'>
        <tr><td><b>Plan:</b></td><td>{plan}</td></tr>
        <tr><td><b>Unidad:</b></td><td>{unidad}</td></tr>
        <tr><td><b>Monto:</b></td><td>${monto}</td></tr>
        <tr><td><b>Vencimiento:</b></td><td>{venc}</td></tr>
    </table>
    {llamadaSafe}
    <p>Saludos,<br/>Equipo GymFlow</p>
</body>
</html>";
    }
}
