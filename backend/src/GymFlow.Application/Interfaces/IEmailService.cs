namespace GymFlow.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Envía un email. Si el servicio está deshabilitado en configuración,
    /// solo se registra en el log y se devuelve éxito sin enviar realmente.
    /// </summary>
    /// <returns>True si se envió (o se simuló envío con éxito), false si falló.</returns>
    Task<EmailResultado> EnviarAsync(string destinatario, string asunto, string cuerpoHtml);
}

public record EmailResultado(bool Exitoso, string? Error = null);
