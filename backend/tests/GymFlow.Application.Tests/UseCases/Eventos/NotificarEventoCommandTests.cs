using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Eventos;

public class NotificarEventoCommandTests
{
    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IEmailService> _emailService = new();

    private NotificarEventoCommand CrearCommand() =>
        new(_eventoRepo.Object, _socioRepo.Object, _auditLogger.Object, _emailService.Object);

    private static Socio CrearSocio(string correo) =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: "Maria",
            apellido: "Lopez",
            correo: correo,
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);

    private static Evento CrearEvento(Unidad unidad)
    {
        var evento = new Evento("Torneo de verano", "desc", DateTime.UtcNow.AddDays(10), unidad.Id);
        typeof(Evento).GetProperty(nameof(Evento.Unidad))!.SetValue(evento, unidad);
        return evento;
    }

    [Fact]
    public async Task ExecuteAsync_EnviaEmailASociosDeLaUnidad()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var evento = CrearEvento(unidad);
        var socios = new[] { CrearSocio("s1@test.com"), CrearSocio("s2@test.com") };

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _socioRepo.Setup(r => r.GetActivosByUnidadAsync(unidad.Id)).ReturnsAsync(socios);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(evento.Id, Guid.NewGuid(), "Admin Test");

        _socioRepo.Verify(r => r.GetActivosByUnidadAsync(unidad.Id), Times.Once);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(socios.Length));
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            It.IsAny<TipoAccionAuditoria>(), "Evento", evento.Id,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SiEmailFalla_NoLanzaYAuditaFallidos()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var evento = CrearEvento(unidad);
        var socios = new[] { CrearSocio("s1@test.com") };

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _socioRepo.Setup(r => r.GetActivosByUnidadAsync(unidad.Id)).ReturnsAsync(socios);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "SMTP timeout"));

        await CrearCommand().ExecuteAsync(evento.Id, Guid.NewGuid(), "Admin Test");

        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            It.IsAny<TipoAccionAuditoria>(), "Evento", evento.Id,
            It.Is<string>(s => s.Contains("fallaron")), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EventoNoExiste_LanzaKeyNotFound()
    {
        _eventoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Evento?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
