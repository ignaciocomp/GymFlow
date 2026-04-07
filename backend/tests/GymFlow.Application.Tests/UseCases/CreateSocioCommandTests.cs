using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Socios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases;

public class CreateSocioCommandTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IUnidadRepository> _unidadRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private CreateSocioCommand CrearCommand() =>
        new(_socioRepo.Object, _unidadRepo.Object, _planRepo.Object, _auditLogger.Object);

    private static Socio SocioFake(TipoDocumento tipo, string? doc) =>
        new("Juan", "García", "juan@test.com", "PENDING_OAUTH",
            DateTime.UtcNow, true, tipo, null, doc, null);

    private void ConfigurarMocksBase(Guid unidadId, TipoDocumento tipo, string? doc)
    {
        _socioRepo.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>())).ReturnsAsync(false);
        _unidadRepo.Setup(r => r.GetByIdAsync(unidadId))
            .ReturnsAsync(new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo"));
        _socioRepo.Setup(r => r.AddAsync(It.IsAny<Socio>())).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(SocioFake(tipo, doc));
    }

    [Fact]
    public async Task ExecuteAsync_ConCI_YCedulaValida_RetornaDtoConTipoDocumentoCorrecto()
    {
        var unidadId = Guid.NewGuid();
        ConfigurarMocksBase(unidadId, TipoDocumento.CI, "54321163");

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.CI,
            DocumentoIdentidad: "54321163",
            FechaNacimiento: null,
            Unidades: [new UnidadAsignacionDto(unidadId, null)],
            ConsentimientoInformado: true);

        var result = await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Test Admin");

        Assert.Equal(TipoDocumento.CI, result.TipoDocumento);
        Assert.Equal("54321163", result.DocumentoIdentidad);
    }

    [Fact]
    public async Task ExecuteAsync_ConCI_YCedulaInvalida_LanzaArgumentException()
    {
        var unidadId = Guid.NewGuid();
        // Solo necesitamos los mocks previos a la creación del Socio
        _socioRepo.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>())).ReturnsAsync(false);
        _unidadRepo.Setup(r => r.GetByIdAsync(unidadId))
            .ReturnsAsync(new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo"));

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.CI,
            DocumentoIdentidad: "12345678",  // inválida: suma=148, verificador esperado=2
            FechaNacimiento: null,
            Unidades: [new UnidadAsignacionDto(unidadId, null)],
            ConsentimientoInformado: true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Test Admin"));
    }

    [Fact]
    public async Task ExecuteAsync_ConPasaporte_RetornaDtoConTipoDocumentoCorrecto()
    {
        var unidadId = Guid.NewGuid();
        ConfigurarMocksBase(unidadId, TipoDocumento.Pasaporte, "XY123456");

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.Pasaporte,
            DocumentoIdentidad: "XY123456",
            FechaNacimiento: null,
            Unidades: [new UnidadAsignacionDto(unidadId, null)],
            ConsentimientoInformado: true);

        var result = await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Test Admin");

        Assert.Equal(TipoDocumento.Pasaporte, result.TipoDocumento);
    }

    [Fact]
    public async Task ExecuteAsync_ConOtro_SinDocumento_RetornaDtoConTipoDocumentoCorrecto()
    {
        var unidadId = Guid.NewGuid();
        ConfigurarMocksBase(unidadId, TipoDocumento.Otro, null);

        var request = new CreateSocioRequest(
            Nombre: "Juan",
            Apellido: "García",
            Correo: "juan@test.com",
            Telefono: null,
            TipoDocumento: TipoDocumento.Otro,
            DocumentoIdentidad: null,
            FechaNacimiento: null,
            Unidades: [new UnidadAsignacionDto(unidadId, null)],
            ConsentimientoInformado: true);

        var result = await CrearCommand().ExecuteAsync(request, Guid.NewGuid(), "Test Admin");

        Assert.Equal(TipoDocumento.Otro, result.TipoDocumento);
        Assert.Null(result.DocumentoIdentidad);
    }
}
