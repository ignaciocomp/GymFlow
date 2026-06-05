using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Inscripciones;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Inscripciones;

public class InscribirSocioCommandTests
{
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();
    private readonly Mock<IClaseRepository> _claseRepo = new();
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private InscribirSocioCommand CrearCommand() =>
        new(_inscripcionRepo.Object, _claseRepo.Object, _cuotaRepo.Object,
            _socioRepo.Object, _emailService.Object, _auditLogger.Object);

    private static Socio CrearSocio() =>
        new(Guid.NewGuid(), "María", "López", "m@test.com", "h", DateTime.UtcNow,
            true, TipoDocumento.CI, null, "12345672", null);

    private static Clase CrearClase(int capacidad = 10) =>
        new("Spinning", "Clase de spinning", capacidad, 60, "Juan", Guid.NewGuid());

    [Fact]
    public async Task ConCuotaVencida_LanzaInvalidOperation()
    {
        var clase = CrearClase();
        var socioId = Guid.NewGuid();

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(socioId, clase.UnidadId)).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CrearCommand().ExecuteAsync(socioId, clase.Id, Guid.NewGuid(), "Admin"));

        _inscripcionRepo.Verify(r => r.AddAsync(It.IsAny<InscripcionClase>()), Times.Never);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConCupo_InscribeNormalEnviaEmailYAudita()
    {
        var clase = CrearClase(capacidad: 10);
        var socio = CrearSocio();

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(socio.Id, clase.UnidadId)).ReturnsAsync(false);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYClaseAsync(socio.Id, clase.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(clase.Id)).ReturnsAsync(3);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var dto = await CrearCommand().ExecuteAsync(socio.Id, clase.Id, Guid.NewGuid(), "Admin");

        Assert.False(dto.EnListaEspera);
        _inscripcionRepo.Verify(r => r.AddAsync(It.Is<InscripcionClase>(i => i.EsListaEspera == false)), Times.Once);
        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.IsAny<Guid>(), "Admin",
            TipoAccionAuditoria.Creacion, "Inscripcion", It.IsAny<Guid?>(), It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task SinCupo_CreaEnListaEspera()
    {
        var clase = CrearClase(capacidad: 5);
        var socio = CrearSocio();

        _claseRepo.Setup(r => r.GetByIdAsync(clase.Id)).ReturnsAsync(clase);
        _cuotaRepo.Setup(r => r.TieneCuotasVencidasEnUnidadAsync(socio.Id, clase.UnidadId)).ReturnsAsync(false);
        _inscripcionRepo.Setup(r => r.GetActivaBySocioYClaseAsync(socio.Id, clase.Id)).ReturnsAsync((InscripcionClase?)null);
        _inscripcionRepo.Setup(r => r.GetInscripcionesActivasCountAsync(clase.Id)).ReturnsAsync(5);
        _socioRepo.Setup(r => r.GetByIdAsync(socio.Id)).ReturnsAsync(socio);
        _inscripcionRepo.Setup(r => r.GetPosicionEnListaEsperaAsync(It.IsAny<Guid>())).ReturnsAsync(1);

        var dto = await CrearCommand().ExecuteAsync(socio.Id, clase.Id, Guid.NewGuid(), "Admin");

        Assert.True(dto.EnListaEspera);
        _inscripcionRepo.Verify(r => r.AddAsync(It.Is<InscripcionClase>(i => i.EsListaEspera == true)), Times.Once);
    }
}
