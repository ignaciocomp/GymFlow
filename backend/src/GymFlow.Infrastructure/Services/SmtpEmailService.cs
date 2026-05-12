using System.Net;
using System.Net.Mail;
using GymFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Implementación de IEmailService que respeta el flag Email:Habilitado en configuración.
/// Si está deshabilitado (default en dev), solo loguea sin enviar el email real.
/// Si está habilitado, usa SMTP con la configuración de appsettings.json.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmailResultado> EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var habilitado = _configuration.GetValue<bool>("Email:Habilitado");

        if (!habilitado)
        {
            _logger.LogInformation(
                "[EMAIL SIMULADO] Para: {Destinatario} | Asunto: {Asunto}",
                destinatario, asunto);
            return new EmailResultado(Exitoso: true);
        }

        try
        {
            var host = _configuration["Email:SmtpHost"]
                ?? throw new InvalidOperationException("Email:SmtpHost no configurado.");
            var port = _configuration.GetValue<int>("Email:SmtpPort");
            var user = _configuration["Email:SmtpUser"]
                ?? throw new InvalidOperationException("Email:SmtpUser no configurado.");
            var pass = _configuration["Email:SmtpPassword"]
                ?? throw new InvalidOperationException("Email:SmtpPassword no configurado.");
            var from = _configuration["Email:From"] ?? user;
            var enableSsl = _configuration.GetValue("Email:EnableSsl", true);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = enableSsl,
            };

            using var mensaje = new MailMessage(from, destinatario, asunto, cuerpoHtml)
            {
                IsBodyHtml = true,
            };

            await client.SendMailAsync(mensaje);

            _logger.LogInformation("Email enviado a {Destinatario} con asunto '{Asunto}'.", destinatario, asunto);
            return new EmailResultado(Exitoso: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló envío de email a {Destinatario}.", destinatario);
            return new EmailResultado(Exitoso: false, Error: ex.Message);
        }
    }
}
