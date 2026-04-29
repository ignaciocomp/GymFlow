using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Socios;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases;

public class AuditLoggingTests
{
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<IUnidadRepository> _unidadRepo = new();
    private readonly Mock<IPlanRepository> _planRepo = new();
    private readonly Mock<IRolRepository> _rolRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private CreateSocioCommand BuildCreateCommand()
    {
        _rolRepo.Setup(r => r.GetByNombreAsync("Socio", default))
            .ReturnsAsync(new Rol(Guid.NewGuid(), "Socio", true, DateTime.UtcNow));
        return new CreateSocioCommand(_socioRepo.Object, _unidadRepo.Object, _planRepo.Object, _rolRepo.Object, _auditLogger.Object);
    }

    private static readonly Guid TestUserId = Guid.NewGuid();
    private const string TestUserName = "Maurice Admin";

    private static Socio SocioFake() =>
        new(Guid.NewGuid(), "Juan", "García", "juan@test.com", null,
            DateTime.UtcNow, true, TipoDocumento.Otro);

    private void ConfigurarMocksBase(Guid unidadId)
    {
        _socioRepo.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>())).ReturnsAsync(false);
        _socioRepo.Setup(r => r.ExisteCedulaAsync(It.IsAny<string>())).ReturnsAsync(false);
        _unidadRepo.Setup(r => r.GetByIdAsync(unidadId))
            .ReturnsAsync(new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo"));
        _socioRepo.Setup(r => r.AddAsync(It.IsAny<Socio>())).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _socioRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(SocioFake());
        _auditLogger.Setup(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoAccionAuditoria>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateSocio_LogsCreacion()
    {
        var unidadId = Guid.NewGuid();
        ConfigurarMocksBase(unidadId);

        var command = BuildCreateCommand();

        var request = new CreateSocioRequest(
            "Juan", "García", "juan@test.com", null,
            TipoDocumento.Otro, null, null, [new UnidadAsignacionDto(unidadId, null)], true);

        await command.ExecuteAsync(request, TestUserId, TestUserName);

        _auditLogger.Verify(a => a.LogAsync(
            TestUserId,
            TestUserName,
            TipoAccionAuditoria.Creacion,
            "Socio",
            It.IsAny<Guid>(),
            It.Is<string>(s => s.Contains("Juan") && s.Contains("García")),
            null),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSocio_LogsBaja()
    {
        var socioId = Guid.NewGuid();
        var socio = SocioFake();
        _socioRepo.Setup(r => r.GetByIdAsync(socioId)).ReturnsAsync(socio);
        _socioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditLogger.Setup(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoAccionAuditoria>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var command = new DeleteSocioCommand(_socioRepo.Object, _auditLogger.Object);

        await command.ExecuteAsync(socioId, "Se mudó", TestUserId, TestUserName);

        _auditLogger.Verify(a => a.LogAsync(
            TestUserId,
            TestUserName,
            TipoAccionAuditoria.Baja,
            "Socio",
            socioId,
            It.Is<string>(s => s.Contains("Se mudó")),
            null),
            Times.Once);
    }

    [Fact]
    public async Task ReactivateSocio_LogsReactivacion()
    {
        var socioId = Guid.NewGuid();
        var socio = SocioFake();
        socio.DarDeBaja("Test");
        _socioRepo.Setup(r => r.GetByIdAsync(socioId)).ReturnsAsync(socio);
        _socioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditLogger.Setup(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoAccionAuditoria>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var command = new ReactivateSocioCommand(_socioRepo.Object, _auditLogger.Object);

        await command.ExecuteAsync(socioId, TestUserId, TestUserName);

        _auditLogger.Verify(a => a.LogAsync(
            TestUserId,
            TestUserName,
            TipoAccionAuditoria.Reactivacion,
            "Socio",
            socioId,
            It.Is<string>(s => s.Contains("Juan") && s.Contains("García")),
            null),
            Times.Once);
    }

    [Fact]
    public async Task CreateSocio_CuandoFalla_NoLogea()
    {
        _socioRepo.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>())).ReturnsAsync(true);

        var command = BuildCreateCommand();

        var request = new CreateSocioRequest(
            "Juan", "García", "duplicado@test.com", null,
            TipoDocumento.Otro, null, null, [new UnidadAsignacionDto(Guid.NewGuid(), null)], true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => command.ExecuteAsync(request, TestUserId, TestUserName));

        _auditLogger.Verify(a => a.LogAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TipoAccionAuditoria>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }
}
