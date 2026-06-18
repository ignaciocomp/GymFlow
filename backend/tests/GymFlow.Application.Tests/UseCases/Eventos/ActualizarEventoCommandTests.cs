using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Eventos;

public class ActualizarEventoCommandTests
{
    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private ActualizarEventoCommand CrearCommand() =>
        new(_eventoRepo.Object, _auditLogger.Object);

    private static Evento CrearEvento(out Unidad unidad)
    {
        unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var evento = new Evento("Torneo original", "desc vieja", DateTime.UtcNow.AddDays(10), unidad.Id);
        // Poblar la navegación de Unidad por reflexión (setter privado) para que el DTO la incluya.
        typeof(Evento).GetProperty(nameof(Evento.Unidad))!.SetValue(evento, unidad);
        return evento;
    }

    [Fact]
    public async Task ExecuteAsync_EventoExistente_CambiaCamposYAudita()
    {
        var evento = CrearEvento(out var unidad);
        var request = new UpdateEventoRequest("Torneo de invierno", "Nueva descripción",
            DateTime.UtcNow.AddDays(20));

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _eventoRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await CrearCommand().ExecuteAsync(evento.Id, request, Guid.NewGuid(), "Admin Test");

        Assert.Equal("Torneo de invierno", dto.Titulo);
        Assert.Equal("Nueva descripción", dto.Descripcion);
        Assert.Equal("Gimnasio Nuevo Malvin", dto.UnidadNombre);
        Assert.Equal("Torneo de invierno", evento.Titulo);

        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Modificacion, "Evento", evento.Id,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PermiteFechaPasada()
    {
        var evento = CrearEvento(out _);
        var fechaPasada = DateTime.UtcNow.AddDays(-5);
        var request = new UpdateEventoRequest("Torneo", "desc", fechaPasada);

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _eventoRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await CrearCommand().ExecuteAsync(evento.Id, request, Guid.NewGuid(), "Admin");

        Assert.Equal(fechaPasada, dto.Fecha);
        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EventoNoExiste_LanzaKeyNotFound()
    {
        var request = new UpdateEventoRequest("Torneo", "desc", DateTime.UtcNow.AddDays(5));
        _eventoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Evento?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), request, Guid.NewGuid(), "Admin"));

        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
