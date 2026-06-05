using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Inscripciones;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Inscripciones;

public class CancelarInscripcionCommandTests
{
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private CancelarInscripcionCommand CrearCommand() =>
        new(_inscripcionRepo.Object, _claseRepo.Object, _emailService.Object, _auditLogger.Object);

    private static Socio CrearSocio() =>
        new(Guid.NewGuid(), "Ana", "Pérez", "a@test.com", "h", DateTime.UtcNow,
            true, TipoDocumento.CI, null, "12345672", null);

    private static Clase CrearClase() =>
        new("Yoga", "desc", 10, 60, "Laura", Guid.NewGuid());

    [Fact]
    public async Task NoEsDueño_LanzaInvalidOperation()
    {
        var claseId = Guid.NewGuid();
        var inscripcion = new InscripcionClase(claseId, Guid.NewGuid());
        _inscripcionRepo.Setup(r => r.GetByIdAsync(inscripcion.Id)).ReturnsAsync(inscripcion);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(inscripcion.Id, Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<TipoAccionAuditoria>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task CancelaYPromueveListaEspera()
    {
        var socioId = Guid.NewGuid();
        var clase = CrearClase();
        var inscripcion = new InscripcionClase(clase.Id, socioId);

        var socioFake = CrearSocio();
        var enEspera = new InscripcionClase(clase.Id, socioFake.Id, esListaEspera: true);
        typeof(InscripcionClase).GetProperty("Socio")!.SetValue(enEspera, socioFake);

        _inscripcionRepo.Setup(r => r.GetByIdAsync(inscripcion.Id)).ReturnsAsync(inscripcion);
        _inscripcionRepo.Setup(r => r.GetPrimeroEnListaEsperaAsync(clase.Id)).ReturnsAsync(enEspera);
        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _emailService.Setup(s => s.EnviarAsync(socioFake.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(inscripcion.Id, socioId, Guid.NewGuid(), "Admin");

        Assert.False(inscripcion.EstaActiva);
        Assert.False(enEspera.EsListaEspera);
        _emailService.Verify(s => s.EnviarAsync(socioFake.Correo, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin",
            TipoAccionAuditoria.Modificacion, "Inscripcion", enEspera.Id, It.IsAny<string>(), null), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin",
            TipoAccionAuditoria.Baja, "Inscripcion", inscripcion.Id, It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task CancelaSinListaEspera_SoloAudita()
    {
        var socioId = Guid.NewGuid();
        var clase = CrearClase();
        var inscripcion = new InscripcionClase(clase.Id, socioId);

        _inscripcionRepo.Setup(r => r.GetByIdAsync(inscripcion.Id)).ReturnsAsync(inscripcion);
        _inscripcionRepo.Setup(r => r.GetPrimeroEnListaEsperaAsync(clase.Id)).ReturnsAsync((InscripcionClase?)null);

        await CrearCommand().ExecuteAsync(inscripcion.Id, socioId, Guid.NewGuid(), "Admin");

        Assert.False(inscripcion.EstaActiva);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin",
            TipoAccionAuditoria.Baja, "Inscripcion", inscripcion.Id, It.IsAny<string>(), null), Times.Once);
    }
}
