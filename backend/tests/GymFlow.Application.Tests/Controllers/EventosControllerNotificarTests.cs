using System.Security.Claims;
using GymFlow.API.Controllers;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GymFlow.Application.Tests.Controllers;

/// <summary>
/// Cubre que la acción de notificar y el endpoint de destinatarios informen
/// a cuántos socios (y de qué sede) se envía el correo del evento (issue #51).
/// </summary>
public class EventosControllerNotificarTests
{
    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IEmailService> _emailService = new();

    private EventosController CrearController()
    {
        var notificarCommand = new NotificarEventoCommand(
            _eventoRepo.Object, _socioRepo.Object, _auditLogger.Object, _emailService.Object);
        var getByIdQuery = new GetEventoByIdQuery(_eventoRepo.Object);

        var controller = new EventosController(
            getEventosQuery: null!,
            getEventoByIdQuery: getByIdQuery,
            crearEventoCommand: null!,
            actualizarEventoCommand: null!,
            cancelarEventoCommand: null!,
            notificarEventoCommand: notificarCommand,
            unidadesResolver: null!,
            socioRepository: _socioRepo.Object);

        // El endpoint de notificar lee NameIdentifier/nombre/apellido del JWT.
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("nombre", "Admin"),
            new Claim("apellido", "Test"),
        }, "test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user },
        };
        return controller;
    }

    private static Socio CrearSocio() =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: "Maria",
            apellido: "Lopez",
            correo: "s@test.com",
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
    public async Task Notificar_MensajeIncluyeCantidadDeSociosYNombreDeSede()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var evento = CrearEvento(unidad);
        var socios = new[] { CrearSocio(), CrearSocio() };

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _socioRepo.Setup(r => r.GetActivosByUnidadAsync(unidad.Id)).ReturnsAsync(socios);
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var result = await CrearController().Notificar(evento.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var mensaje = (string)ok.Value!.GetType().GetProperty("mensaje")!.GetValue(ok.Value)!;
        Assert.Contains("2", mensaje);
        Assert.Contains("Gimnasio Nuevo Malvin", mensaje);
    }

    [Fact]
    public async Task Notificar_SinSociosActivos_MensajeLoIndica()
    {
        var unidad = new Unidad("Gimnasio Centro", "Centro, Montevideo");
        var evento = CrearEvento(unidad);

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _socioRepo.Setup(r => r.GetActivosByUnidadAsync(unidad.Id)).ReturnsAsync(Array.Empty<Socio>());

        var result = await CrearController().Notificar(evento.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var mensaje = (string)ok.Value!.GetType().GetProperty("mensaje")!.GetValue(ok.Value)!;
        Assert.Contains("No hay socios", mensaje);
        Assert.Contains("Gimnasio Centro", mensaje);
    }

    [Fact]
    public async Task Notificar_EventoNoExiste_RetornaNotFound()
    {
        _eventoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Evento?)null);

        var result = await CrearController().Notificar(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDestinatarios_RetornaCantidadYSede()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var evento = CrearEvento(unidad);

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _socioRepo.Setup(r => r.CountActivosByUnidadAsync(unidad.Id)).ReturnsAsync(5);

        var result = await CrearController().GetDestinatarios(evento.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var cantidad = (int)ok.Value!.GetType().GetProperty("cantidad")!.GetValue(ok.Value)!;
        var sede = (string)ok.Value!.GetType().GetProperty("sede")!.GetValue(ok.Value)!;
        Assert.Equal(5, cantidad);
        Assert.Equal("Gimnasio Nuevo Malvin", sede);
    }

    [Fact]
    public async Task GetDestinatarios_EventoNoExiste_RetornaNotFound()
    {
        _eventoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Evento?)null);

        var result = await CrearController().GetDestinatarios(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
