using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Dashboard;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Dashboard;

/// <summary>
/// GetDashboardQuery (RF-18 / CU-10): consolidado por defecto (RN-14), filtro por unidad,
/// restricción server-side del Dueño (RN-16), métricas de cuotas en vivo (RN-17) y series
/// de la gráfica. unidadesPermitidas: null = sin restricción; VACÍA = dueño sin unidades
/// (todo en cero); unidadId fuera de las permitidas = UnauthorizedAccessException.
/// </summary>
public class GetDashboardQueryTests
{
    private readonly Mock<IUnidadRepository> _unidadRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();
    private readonly Mock<ICuotaRepository> _cuotaRepo = new();
    private readonly Mock<IHorarioClaseRepository> _horarioRepo = new();
    private readonly Mock<IInscripcionClaseRepository> _inscripcionRepo = new();

    private readonly Unidad _mora = new("Espacio Mora", "Dir 1");
    private readonly Unidad _sayago = new("Espacio Sayago", "Dir 2");

    private static readonly DateTime Hoy = DateTime.UtcNow.Date;

    private static DiaSemana DiaDeHoy =>
        DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday ? DiaSemana.Domingo : (DiaSemana)(int)DateTime.UtcNow.DayOfWeek;

    public GetDashboardQueryTests()
    {
        _unidadRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { _mora, _sayago });
        _horarioRepo.Setup(r => r.GetByDiaAsync(It.IsAny<DiaSemana>(), It.IsAny<IReadOnlyCollection<Guid>?>()))
            .ReturnsAsync(Array.Empty<HorarioClase>());
        _inscripcionRepo.Setup(r => r.GetRecientesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<Guid>?>()))
            .ReturnsAsync(Array.Empty<InscripcionClase>());
        _inscripcionRepo.Setup(r => r.GetConteoActivasPorDiaAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>()))
            .ReturnsAsync(new Dictionary<DateTime, int>());
        _inscripcionRepo.Setup(r => r.GetConteoActivasPorHorariosAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
    }

    private GetDashboardQuery CrearQuery() => new(
        _unidadRepo.Object,
        _socioRepo.Object,
        _cuotaRepo.Object,
        _horarioRepo.Object,
        _inscripcionRepo.Object);

    private (Clase Clase, HorarioClase Horario) CrearClaseConHorario(Unidad unidad, DiaSemana dia)
    {
        var clase = new Clase("Funcional", "desc", 20, 60, "Ana", unidad.Id);
        typeof(Clase).GetProperty(nameof(Clase.Unidad))!.SetValue(clase, unidad);
        var horario = new HorarioClase(clase.Id, dia, new TimeOnly(8, 0), new TimeOnly(9, 0), null);
        typeof(HorarioClase).GetProperty(nameof(HorarioClase.Clase))!.SetValue(horario, clase);
        return (clase, horario);
    }

    private static Socio CrearSocio(string nombre, string apellido) =>
        new(rolSocioId: Guid.NewGuid(),
            nombre: nombre,
            apellido: apellido,
            correo: $"{Guid.NewGuid():N}@test.com",
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.Pasaporte);

    // --- Consolidado (RN-14) ---

    [Fact]
    public async Task ExecuteAsync_SinFiltros_ConsolidaTodasLasUnidades()
    {
        _socioRepo.Setup(r => r.CountActivosAsync(null)).ReturnsAsync(30);
        _socioRepo.Setup(r => r.CountActivosByUnidadAsync(_mora.Id)).ReturnsAsync(18);
        _socioRepo.Setup(r => r.CountActivosByUnidadAsync(_sayago.Id)).ReturnsAsync(12);
        _cuotaRepo.Setup(r => r.CountPendientesPorVencerAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(5);
        _cuotaRepo.Setup(r => r.CountPendientesVencidasAsync(It.IsAny<DateTime>(), null)).ReturnsAsync(3);
        _cuotaRepo.Setup(r => r.CountPagadasDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), null)).ReturnsAsync(7);

        var dto = await CrearQuery().ExecuteAsync();

        Assert.Equal(30, dto.SociosActivos.Total);
        Assert.Equal(2, dto.SociosActivos.PorUnidad.Count);
        Assert.Equal(18, dto.SociosActivos.PorUnidad.Single(p => p.UnidadId == _mora.Id).Cantidad);
        Assert.Equal(12, dto.SociosActivos.PorUnidad.Single(p => p.UnidadId == _sayago.Id).Cantidad);
        Assert.Equal(5, dto.Cuotas.ProximasAVencer);
        Assert.Equal(3, dto.Cuotas.Vencidas);
        Assert.Equal(7, dto.Cuotas.PagadasMes);
        Assert.Equal(2, dto.Unidades.Count);
        Assert.True((DateTime.UtcNow - dto.GeneradoEn).Duration() < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ExecuteAsync_UsaVentanaDeCincoDiasYMesActualParaCuotas()
    {
        await CrearQuery().ExecuteAsync();

        // Próximas a vencer = Pendiente con vencimiento en [hoy, hoy+5]; pagadas del mes = mes actual (UTC).
        _cuotaRepo.Verify(r => r.CountPendientesPorVencerAsync(Hoy, Hoy.AddDays(5), null), Times.Once);
        _cuotaRepo.Verify(r => r.CountPendientesVencidasAsync(Hoy, null), Times.Once);
        _cuotaRepo.Verify(r => r.CountPagadasDelMesAsync(Hoy.Year, Hoy.Month, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MapeaClasesDelDiaConCupoEInscriptos()
    {
        var (_, horario) = CrearClaseConHorario(_mora, DiaDeHoy);
        _horarioRepo.Setup(r => r.GetByDiaAsync(DiaDeHoy, null)).ReturnsAsync(new[] { horario });
        _inscripcionRepo.Setup(r => r.GetConteoActivasPorHorariosAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(horario.Id))))
            .ReturnsAsync(new Dictionary<Guid, int> { [horario.Id] = 12 });

        var dto = await CrearQuery().ExecuteAsync();

        var claseDia = Assert.Single(dto.ClasesDelDia);
        Assert.Equal("Funcional", claseDia.Clase);
        Assert.Equal("Espacio Mora", claseDia.Unidad);
        Assert.Equal("08:00", claseDia.HoraInicio);
        Assert.Equal("09:00", claseDia.HoraFin);
        Assert.Equal(20, claseDia.Cupo);
        Assert.Equal(12, claseDia.Inscriptos);

        // El día consultado es el día de la semana actual (mapeo DayOfWeek → DiaSemana en UTC).
        _horarioRepo.Verify(r => r.GetByDiaAsync(DiaDeHoy, null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MapeaInscripcionesRecientes()
    {
        var (_, horario) = CrearClaseConHorario(_mora, DiaDeHoy);
        var socio = CrearSocio("Maria", "Lopez");
        var inscripcion = new InscripcionClase(horario.Id, socio.Id);
        typeof(InscripcionClase).GetProperty(nameof(InscripcionClase.Socio))!.SetValue(inscripcion, socio);
        typeof(InscripcionClase).GetProperty(nameof(InscripcionClase.HorarioClase))!.SetValue(inscripcion, horario);

        _inscripcionRepo.Setup(r => r.GetRecientesAsync(10, null)).ReturnsAsync(new[] { inscripcion });

        var dto = await CrearQuery().ExecuteAsync();

        var reciente = Assert.Single(dto.InscripcionesRecientes);
        Assert.Equal("Maria Lopez", reciente.Socio);
        Assert.Equal("Funcional", reciente.Clase);
        Assert.Equal("Espacio Mora", reciente.Unidad);
        Assert.Equal(inscripcion.FechaInscripcion, reciente.Fecha);
    }

    // --- Filtro por unidad ---

    [Fact]
    public async Task ExecuteAsync_ConUnidadId_FiltraLasMetricasAEsaUnidad()
    {
        _socioRepo.Setup(r => r.CountActivosAsync(It.Is<IReadOnlyCollection<Guid>>(c => c.Count == 1 && c.Contains(_sayago.Id))))
            .ReturnsAsync(12);
        _socioRepo.Setup(r => r.CountActivosByUnidadAsync(_sayago.Id)).ReturnsAsync(12);

        var dto = await CrearQuery().ExecuteAsync(unidadId: _sayago.Id);

        Assert.Equal(12, dto.SociosActivos.Total);
        var porUnidad = Assert.Single(dto.SociosActivos.PorUnidad);
        Assert.Equal(_sayago.Id, porUnidad.UnidadId);
        // El selector de unidades sigue mostrando todas las visibles.
        Assert.Equal(2, dto.Unidades.Count);

        _cuotaRepo.Verify(r => r.CountPendientesVencidasAsync(Hoy,
            It.Is<IReadOnlyCollection<Guid>>(c => c.Count == 1 && c.Contains(_sayago.Id))), Times.Once);
        _horarioRepo.Verify(r => r.GetByDiaAsync(DiaDeHoy,
            It.Is<IReadOnlyCollection<Guid>>(c => c.Count == 1 && c.Contains(_sayago.Id))), Times.Once);
    }

    // --- Dueño (RN-16) ---

    [Fact]
    public async Task ExecuteAsync_DuenoRestringido_SoloVeSusUnidades()
    {
        var permitidas = new[] { _mora.Id };
        _socioRepo.Setup(r => r.CountActivosAsync(permitidas)).ReturnsAsync(18);
        _socioRepo.Setup(r => r.CountActivosByUnidadAsync(_mora.Id)).ReturnsAsync(18);

        var dto = await CrearQuery().ExecuteAsync(unidadId: null, unidadesPermitidas: permitidas);

        var unidad = Assert.Single(dto.Unidades);
        Assert.Equal(_mora.Id, unidad.Id);
        var porUnidad = Assert.Single(dto.SociosActivos.PorUnidad);
        Assert.Equal(_mora.Id, porUnidad.UnidadId);
        Assert.Equal(18, dto.SociosActivos.Total);

        _cuotaRepo.Verify(r => r.CountPendientesPorVencerAsync(Hoy, Hoy.AddDays(5), permitidas), Times.Once);
        _inscripcionRepo.Verify(r => r.GetRecientesAsync(10, permitidas), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UnidadNoPermitida_LanzaUnauthorized()
    {
        var permitidas = new[] { _mora.Id };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearQuery().ExecuteAsync(unidadId: _sayago.Id, unidadesPermitidas: permitidas));
    }

    [Fact]
    public async Task ExecuteAsync_DuenoSinUnidades_TodoEnCeroSinConsultarMetricas()
    {
        // Colección VACÍA (distinto de null): dueño sin unidades asignadas → métricas en cero (E3).
        var dto = await CrearQuery().ExecuteAsync(unidadId: null, unidadesPermitidas: Array.Empty<Guid>());

        Assert.Empty(dto.Unidades);
        Assert.Equal(0, dto.SociosActivos.Total);
        Assert.Empty(dto.SociosActivos.PorUnidad);
        Assert.Equal(0, dto.Cuotas.ProximasAVencer);
        Assert.Equal(0, dto.Cuotas.Vencidas);
        Assert.Equal(0, dto.Cuotas.PagadasMes);
        Assert.Empty(dto.ClasesDelDia);
        Assert.Empty(dto.InscripcionesRecientes);
        Assert.Empty(dto.Grafica.SociosPorSede);
        Assert.Equal(7, dto.Grafica.InscripcionesUltimos7Dias.Count);
        Assert.All(dto.Grafica.InscripcionesUltimos7Dias, p => Assert.Equal(0, p.Cantidad));
        Assert.All(dto.Grafica.CuotasPorEstado, p => Assert.Equal(0, p.Cantidad));

        _socioRepo.Verify(r => r.CountActivosAsync(It.IsAny<IReadOnlyCollection<Guid>?>()), Times.Never);
        _cuotaRepo.Verify(r => r.CountPendientesVencidasAsync(It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>()), Times.Never);
        _horarioRepo.Verify(r => r.GetByDiaAsync(It.IsAny<DiaSemana>(), It.IsAny<IReadOnlyCollection<Guid>?>()), Times.Never);
    }

    // --- Gráfica ---

    [Fact]
    public async Task ExecuteAsync_SeriesDeLaGraficaCorrectas()
    {
        _socioRepo.Setup(r => r.CountActivosAsync(null)).ReturnsAsync(30);
        _socioRepo.Setup(r => r.CountActivosByUnidadAsync(_mora.Id)).ReturnsAsync(18);
        _socioRepo.Setup(r => r.CountActivosByUnidadAsync(_sayago.Id)).ReturnsAsync(12);
        _cuotaRepo.Setup(r => r.CountPendientesPorVencerAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(5);
        _cuotaRepo.Setup(r => r.CountPendientesVencidasAsync(It.IsAny<DateTime>(), null)).ReturnsAsync(3);
        _cuotaRepo.Setup(r => r.CountPagadasDelMesAsync(It.IsAny<int>(), It.IsAny<int>(), null)).ReturnsAsync(7);
        _inscripcionRepo.Setup(r => r.GetConteoActivasPorDiaAsync(Hoy.AddDays(-6), Hoy, null))
            .ReturnsAsync(new Dictionary<DateTime, int> { [Hoy] = 4, [Hoy.AddDays(-3)] = 2 });

        var dto = await CrearQuery().ExecuteAsync();

        // Socios por sede refleja el desglose por unidad.
        Assert.Equal(2, dto.Grafica.SociosPorSede.Count);
        Assert.Equal(18, dto.Grafica.SociosPorSede.Single(s => s.Sede == "Espacio Mora").Cantidad);
        Assert.Equal(12, dto.Grafica.SociosPorSede.Single(s => s.Sede == "Espacio Sayago").Cantidad);

        // Cuotas por estado con los tres estados del resumen.
        Assert.Equal(3, dto.Grafica.CuotasPorEstado.Count);
        Assert.Equal(5, dto.Grafica.CuotasPorEstado.Single(c => c.Estado == "Próximas a vencer").Cantidad);
        Assert.Equal(3, dto.Grafica.CuotasPorEstado.Single(c => c.Estado == "Vencidas").Cantidad);
        Assert.Equal(7, dto.Grafica.CuotasPorEstado.Single(c => c.Estado == "Pagadas del mes").Cantidad);

        // Serie de 7 días en orden cronológico, con ceros en los días sin inscripciones.
        var serie = dto.Grafica.InscripcionesUltimos7Dias;
        Assert.Equal(7, serie.Count);
        Assert.Equal(Hoy.AddDays(-6).ToString("yyyy-MM-dd"), serie[0].Fecha);
        Assert.Equal(Hoy.ToString("yyyy-MM-dd"), serie[6].Fecha);
        Assert.Equal(2, serie[3].Cantidad);
        Assert.Equal(4, serie[6].Cantidad);
        Assert.Equal(0, serie[0].Cantidad);
        Assert.Equal(6, serie.Sum(p => p.Cantidad)); // 2 + 4 + ceros
    }
}
