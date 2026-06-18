using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Cuotas;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Cuotas;

public class ProcesarRecordatoriosCommandTests
{
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<IRecordatorioCuotaRepository> _recordatorioRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificadorInApp> _notificador = new();

    private ProcesarRecordatoriosCommand CrearCommand() =>
        new(_cuotaRepo.Object, _recordatorioRepo.Object, _emailService.Object, _notificador.Object);

    [Theory]
    [InlineData(5, TipoRecordatorio.CincoDias)]
    [InlineData(1, TipoRecordatorio.UnDia)]
    [InlineData(0, TipoRecordatorio.DiaVencimiento)]
    public void ResolverTipo_DiferenciaCorrespondiente_RetornaTipoEsperado(int diffDias, TipoRecordatorio esperado)
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var vencimiento = hoy.AddDays(diffDias);

        var resultado = ProcesarRecordatoriosCommand.ResolverTipo(vencimiento, hoy);

        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(-1)]
    [InlineData(100)]
    public void ResolverTipo_DiferenciaNoCorresponde_RetornaNull(int diffDias)
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var vencimiento = hoy.AddDays(diffDias);

        var resultado = ProcesarRecordatoriosCommand.ResolverTipo(vencimiento, hoy);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ExecuteAsync_SinCuotas_RetornaCerosYNoEnviaEmails()
    {
        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Cuota>());

        var resultado = await CrearCommand().ExecuteAsync(DateTime.UtcNow);

        Assert.Equal(0, resultado.Enviados);
        Assert.Equal(0, resultado.Omitidos);
        Assert.Equal(0, resultado.Fallidos);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaVenceEn5Dias_EnviaEmailTipoCincoDias()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (socio, cuota) = CrearCuotaConSocio(vencimiento: hoy.AddDays(5));

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { cuota });
        _recordatorioRepo.Setup(r => r.ExisteRecordatorioHoyAsync(cuota.Id, TipoRecordatorio.CincoDias)).ReturnsAsync(false);
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var resultado = await CrearCommand().ExecuteAsync(hoy);

        Assert.Equal(1, resultado.Enviados);
        Assert.Equal(0, resultado.Omitidos);
        Assert.Equal(0, resultado.Fallidos);
        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.Is<string>(a => a.Contains("vence pronto")), It.IsAny<string>()), Times.Once);
        _recordatorioRepo.Verify(r => r.AddAsync(It.Is<RecordatorioCuota>(rc =>
            rc.TipoRecordatorio == TipoRecordatorio.CincoDias && rc.Exitoso == true)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaVenceMañana_EnviaEmailTipoUnDia()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (socio, cuota) = CrearCuotaConSocio(vencimiento: hoy.AddDays(1));

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { cuota });
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(hoy);

        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.Is<string>(a => a.Contains("vence mañana")), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuotaVenceHoy_EnviaEmailTipoDiaVencimiento()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (socio, cuota) = CrearCuotaConSocio(vencimiento: hoy);

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { cuota });
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(hoy);

        _emailService.Verify(s => s.EnviarAsync(socio.Correo, It.Is<string>(a => a.Contains("vence hoy")), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RecordatorioYaEnviadoHoy_OmiteSinDuplicar()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (_, cuota) = CrearCuotaConSocio(vencimiento: hoy.AddDays(5));

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { cuota });
        _recordatorioRepo.Setup(r => r.ExisteRecordatorioHoyAsync(cuota.Id, TipoRecordatorio.CincoDias)).ReturnsAsync(true);

        var resultado = await CrearCommand().ExecuteAsync(hoy);

        Assert.Equal(0, resultado.Enviados);
        Assert.Equal(1, resultado.Omitidos);
        _emailService.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _recordatorioRepo.Verify(r => r.AddAsync(It.IsAny<RecordatorioCuota>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EmailFalla_CuentaComoFallidoYRegistraError()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (socio, cuota) = CrearCuotaConSocio(vencimiento: hoy.AddDays(5));

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { cuota });
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "SMTP unavailable"));

        var resultado = await CrearCommand().ExecuteAsync(hoy);

        Assert.Equal(0, resultado.Enviados);
        Assert.Equal(1, resultado.Fallidos);
        _recordatorioRepo.Verify(r => r.AddAsync(It.Is<RecordatorioCuota>(rc =>
            rc.Exitoso == false && rc.Error == "SMTP unavailable")), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MultiplesCuotas_ProcesaTodasYSumaContadores()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (s1, c1) = CrearCuotaConSocio(correo: "a@test.com", vencimiento: hoy.AddDays(5));
        var (s2, c2) = CrearCuotaConSocio(correo: "b@test.com", vencimiento: hoy.AddDays(1));
        var (s3, c3) = CrearCuotaConSocio(correo: "c@test.com", vencimiento: hoy);

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { c1, c2, c3 });
        _emailService.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        var resultado = await CrearCommand().ExecuteAsync(hoy);

        Assert.Equal(3, resultado.Enviados);
        Assert.Equal(0, resultado.Omitidos);
        Assert.Equal(0, resultado.Fallidos);
        _recordatorioRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CreaNotificacionesEnBatch_SoloDeLosEnviados()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (s1, c1) = CrearCuotaConSocio(correo: "a@test.com", vencimiento: hoy.AddDays(5));
        var (s2, c2) = CrearCuotaConSocio(correo: "b@test.com", vencimiento: hoy.AddDays(1));
        // c3 falla el email → NO debe quedar en el batch in-app.
        var (s3, c3) = CrearCuotaConSocio(correo: "c@test.com", vencimiento: hoy);

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { c1, c2, c3 });
        _emailService.Setup(s => s.EnviarAsync(s3.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: false, Error: "SMTP unavailable"));
        _emailService.Setup(s => s.EnviarAsync(s1.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));
        _emailService.Setup(s => s.EnviarAsync(s2.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));

        await CrearCommand().ExecuteAsync(hoy);

        // Un solo batch (después del SaveChanges de los recordatorios) con los 2 enviados.
        _notificador.Verify(n => n.CrearParaVariosAsync(
            It.Is<IEnumerable<Guid>>(ids =>
                ids.Count() == 2 &&
                ids.Contains(s1.Id) &&
                ids.Contains(s2.Id) &&
                !ids.Contains(s3.Id)),
            TipoNotificacion.RecordatorioCuota,
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SinEnviados_NoLlamaAlNotificador()
    {
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new List<Cuota>());

        await CrearCommand().ExecuteAsync(hoy);

        _notificador.Verify(n => n.CrearParaVariosAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<TipoNotificacion>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NotificadorLanza_NoRompeElJob()
    {
        // Best-effort: si el batch in-app falla, el job igual devuelve su resultado.
        var hoy = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        var (socio, cuota) = CrearCuotaConSocio(vencimiento: hoy.AddDays(5));

        _cuotaRepo.Setup(r => r.GetCuotasParaRecordatorioAsync(hoy)).ReturnsAsync(new[] { cuota });
        _emailService.Setup(s => s.EnviarAsync(socio.Correo, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new EmailResultado(Exitoso: true));
        _notificador.Setup(n => n.CrearParaVariosAsync(It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<TipoNotificacion>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB caída"));

        var resultado = await CrearCommand().ExecuteAsync(hoy);

        Assert.Equal(1, resultado.Enviados);
    }

    private static (Socio Socio, Cuota Cuota) CrearCuotaConSocio(string correo = "socio@test.com", DateTime? vencimiento = null)
    {
        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Test", apellido: "Socio",
            correo: correo, passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);

        var unidad = new Unidad("Espacio Mora", "Av. Italia 5765");
        var emision = (vencimiento ?? DateTime.UtcNow.AddDays(5)).AddMonths(-1);
        var cuota = new Cuota(socio.Id, unidad.Id, Guid.NewGuid(), "Plan Musculación", 2500m, emision);

        // Forzar las nav properties para que el command pueda usarlas (en producción EF las carga)
        typeof(Cuota).GetProperty(nameof(Cuota.Socio))!.SetValue(cuota, socio);
        typeof(Cuota).GetProperty(nameof(Cuota.Unidad))!.SetValue(cuota, unidad);

        return (socio, cuota);
    }
}
