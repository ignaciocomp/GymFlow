using System.Net;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Empleados;

/// <summary>
/// Plantillas de correo relacionadas con la gestión de empleados.
/// </summary>
public static class EmpleadoEmailTemplates
{
    /// <summary>
    /// Arma el correo de bienvenida con las credenciales temporales del empleado.
    /// </summary>
    public static (string Asunto, string Cuerpo) Bienvenida(Empleado empleado, string passwordTemporal, string rolNombre)
    {
        var nombre = WebUtility.HtmlEncode(empleado.Nombre);
        var apellido = WebUtility.HtmlEncode(empleado.Apellido);
        var correo = WebUtility.HtmlEncode(empleado.Correo);
        var rol = WebUtility.HtmlEncode(rolNombre);
        var password = WebUtility.HtmlEncode(passwordTemporal);

        var asunto = "Bienvenido/a a GymFlow";

        var cuerpo = $@"<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""></head>
<body style=""font-family: Arial, sans-serif; color: #1f2937;"">
    <h2>¡Bienvenido/a a GymFlow, {nombre} {apellido}!</h2>
    <p>Se creó tu cuenta de empleado con el rol <strong>{rol}</strong>.</p>
    <p>Estas son tus credenciales temporales para ingresar al sistema:</p>
    <table style=""border-collapse: collapse; margin: 16px 0;"">
        <tr>
            <td style=""padding: 8px 16px; font-weight: bold;"">Correo</td>
            <td style=""padding: 8px 16px;"">{correo}</td>
        </tr>
        <tr>
            <td style=""padding: 8px 16px; font-weight: bold;"">Contraseña temporal</td>
            <td style=""padding: 8px 16px; font-family: monospace;"">{password}</td>
        </tr>
    </table>
    <p><strong>Importante:</strong> por seguridad, cambiá esta contraseña la primera vez que ingreses al sistema.</p>
    <p>Saludos,<br>El equipo de GymFlow</p>
</body>
</html>";

        return (asunto, cuerpo);
    }
}
