using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Inscripciones;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Inscripciones;

public class CancelarInscripcionCommandTests
{
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private CancelarInscripcionCommand CrearCommand() =>
        new(_inscripcionRepo.Object, _emailService.Object, _auditLogger.Object);

    private static Socio CrearSocio() =>
        new(Guid.NewGuid(), "Ana", "Perez", "a@test.com", "h", DateTime.UtcNow,
            true, TipoDocumento.CI, null, "12345672", null);

    private static Clase CrearClase() =>
        new("Yoga", "desc", 10, 60, "Laura", Guid.NewGuid());

    private static HorarioClase CrearHorario(Clase clase)
    {
        var horario = new HorarioClase(clase.Id, DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(9, 0), "Sala 1");
        typeof(HorarioClase).GetProperty("Clase")!.SetValue(horario, clase);
        return horario;
    }

    [Fact]
    public async Task NoEsDueno_LanzaInvalidOperation()
    {
        var inscripcion = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid());
        _inscripcionRepo.Setup(r => r.GetByIdAsync(inscripcion.Id)).ReturnsAsync(inscripcion);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(inscripcion.Id, Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<TipoAccionAuditoria>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task CancelaInscripcion_AuditaBaja()
    {
        var socioId = Guid.NewGuid();
        var clase = CrearClase();
        var horario = CrearHorario(clase);
        var inscripcion = new InscripcionClase(horario.Id, socioId);
        typeof(InscripcionClase).GetProperty("HorarioClase")!.SetValue(inscripcion, horario);

        _inscripcionRepo.Setup(r => r.GetByIdAsync(inscripcion.Id)).ReturnsAsync(inscripcion);

        await CrearCommand().ExecuteAsync(inscripcion.Id, socioId, Guid.NewGuid(), "Admin");

        Assert.False(inscripcion.EstaActiva);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin",
            TipoAccionAuditoria.Baja, "Inscripcion", inscripcion.Id, It.IsAny<string>(), null), Times.Once);
    }
}
