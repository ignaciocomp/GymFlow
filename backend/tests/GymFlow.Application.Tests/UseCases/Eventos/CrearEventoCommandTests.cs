using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Eventos;

public class CrearEventoCommandTests
{
    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<IUnidadRepository> _unidadRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IEmailService> _emailService = new();

    private CrearEventoCommand CrearCommand() =>
        new(_eventoRepo.Object, _unidadRepo.Object, _socioRepo.Object, _auditLogger.Object, _emailService.Object);

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

    [Fact]
    public async Task ExecuteAsync_UnidadExistente_PersisteYNotificaSocios()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var request = new CreateEventoRequest("Torneo de verano", "Torneo abierto",
            DateTime.UtcNow.AddDays(10), unidad.Id);
        var socios = new[] { CrearSocio("s1@test.com"), CrearSocio("s2@test.com") };

        _unidadRepo.Setup(r => r.GetByIdAsync(unidad.Id)).ReturnsAsync(unidad);
        _eventoRepo.Setup(r => r.AddAsync(It.IsAny<Evento>())).Returns(Task.CompletedTask);
        _eventoRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.GetActivosByUnidadAsync(unidad.Id)).ReturnsAsync(socios);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var dto = await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin Test");

        Assert.Equal("Torneo de verano", dto.Titulo);
        Assert.Equal(unidad.Id, dto.UnidadId);
        Assert.Equal("Gimnasio Nuevo Malvin", dto.UnidadNombre);
        Assert.True(dto.EstaActivo);

        _eventoRepo.Verify(r => r.AddAsync(It.IsAny<Evento>()), Times.Once);
        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(socios.Length));
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Creacion, "Evento", It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmailIncluyeLaSede()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var request = new CreateEventoRequest("Torneo de verano", "Torneo abierto",
            DateTime.UtcNow.AddDays(10), unidad.Id);
        var socios = new[] { CrearSocio("s1@test.com") };

        _unidadRepo.Setup(r => r.GetByIdAsync(unidad.Id)).ReturnsAsync(unidad);
        _eventoRepo.Setup(r => r.AddAsync(It.IsAny<Evento>())).Returns(Task.CompletedTask);
        _eventoRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.GetActivosByUnidadAsync(unidad.Id)).ReturnsAsync(socios);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin Test");

        // Regresión: al crear, la navegación Unidad no está poblada, pero el email
        // igual debe llevar el nombre de la sede (asunto y cuerpo).
        _emailService.Verify(s => s.EnviarAsync(
            It.IsAny<string>(),
            It.Is<string>(asunto => asunto.Contains("Gimnasio Nuevo Malvin")),
            It.Is<string>(cuerpo => cuerpo.Contains("Gimnasio Nuevo Malvin"))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UnidadNoExiste_LanzaArgumentExceptionYNoPersiste()
    {
        var request = new CreateEventoRequest("Torneo", "desc",
            DateTime.UtcNow.AddDays(5), Guid.NewGuid());
        _unidadRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Unidad?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin"));

        _eventoRepo.Verify(r => r.AddAsync(It.IsAny<Evento>()), Times.Never);
        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FechaPasada_LanzaArgumentExceptionYNoPersiste()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var request = new CreateEventoRequest("Torneo", "desc",
            DateTime.UtcNow.AddDays(-1), unidad.Id);
        _unidadRepo.Setup(r => r.GetByIdAsync(unidad.Id)).ReturnsAsync(unidad);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin"));

        _eventoRepo.Verify(r => r.AddAsync(It.IsAny<Evento>()), Times.Never);
        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SiEmailFalla_ElEventoIgualSeCrea()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var request = new CreateEventoRequest("Torneo de verano", "Torneo abierto",
            DateTime.UtcNow.AddDays(10), unidad.Id);
        var socios = new[] { CrearSocio("s1@test.com") };

        _unidadRepo.Setup(r => r.GetByIdAsync(unidad.Id)).ReturnsAsync(unidad);
        _eventoRepo.Setup(r => r.AddAsync(It.IsAny<Evento>())).Returns(Task.CompletedTask);
        _eventoRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.GetActivosByUnidadAsync(unidad.Id)).ReturnsAsync(socios);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "SMTP timeout"));

        var dto = await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Admin Test");

        Assert.Equal("Torneo de verano", dto.Titulo);
        // El evento se persiste ANTES de enviar emails: SaveChanges igual se llamo.
        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Creacion, "Evento", It.IsAny<Guid>(),
            It.Is<string>(s => s.Contains("fallaron")), It.IsAny<string?>()), Times.Once);
    }
}
