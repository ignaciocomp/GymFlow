using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Eventos;

public class CancelarEventoCommandTests
{
    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private CancelarEventoCommand CrearCommand() =>
        new(_eventoRepo.Object, _auditLogger.Object);

    [Fact]
    public async Task ExecuteAsync_EventoExistente_BajaLogicaYAudita()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var evento = new Evento("Torneo de verano", "desc", DateTime.UtcNow.AddDays(10), unidad.Id);

        _eventoRepo.Setup(r => r.GetByIdAsync(evento.Id)).ReturnsAsync(evento);
        _eventoRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await CrearCommand().ExecuteAsync(evento.Id, Guid.NewGuid(), "Admin Test");

        Assert.False(evento.EstaActivo);
        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin Test",
            TipoAccionAuditoria.Baja, "Evento", evento.Id,
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EventoNoExiste_LanzaKeyNotFound()
    {
        _eventoRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Evento?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearCommand().ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _eventoRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
