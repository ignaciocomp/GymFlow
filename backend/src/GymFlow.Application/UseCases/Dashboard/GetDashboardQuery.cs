using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Dashboard;

/// <summary>
/// Calcula el snapshot del dashboard (RF-18 / CU-10): socios activos, cuotas en vivo (RN-17),
/// clases del día, inscripciones recientes y series de la gráfica. Consolidado por defecto
/// (RN-14) y filtrable por unidad; el filtrado del Dueño llega resuelto server-side en
/// <c>unidadesPermitidas</c> (RN-16): null = sin restricción, colección vacía = dueño sin
/// unidades (todo en cero), y una <c>unidadId</c> fuera de las permitidas lanza
/// <see cref="UnauthorizedAccessException"/>.
/// </summary>
public class GetDashboardQuery
{
    private const int DiasVentanaProximasAVencer = 5;
    private const int CantidadInscripcionesRecientes = 10;
    private const int DiasSerieInscripciones = 7;

    private readonly IUnidadRepository _unidadRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly ICuotaRepository _cuotaRepository;
    private readonly IHorarioClaseRepository _horarioClaseRepository;
    private readonly IInscripcionClaseRepository _inscripcionClaseRepository;

    public GetDashboardQuery(
        IUnidadRepository unidadRepository,
        ISocioRepository socioRepository,
        ICuotaRepository cuotaRepository,
        IHorarioClaseRepository horarioClaseRepository,
        IInscripcionClaseRepository inscripcionClaseRepository)
    {
        _unidadRepository = unidadRepository;
        _socioRepository = socioRepository;
        _cuotaRepository = cuotaRepository;
        _horarioClaseRepository = horarioClaseRepository;
        _inscripcionClaseRepository = inscripcionClaseRepository;
    }

    public async Task<DashboardDto> ExecuteAsync(
        Guid? unidadId = null,
        IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        if (unidadId.HasValue && unidadesPermitidas is not null && !unidadesPermitidas.Contains(unidadId.Value))
            throw new UnauthorizedAccessException("La unidad solicitada no está entre las unidades visibles del usuario.");

        var todas = (await _unidadRepository.GetAllAsync()).ToList();
        var visibles = unidadesPermitidas is null
            ? todas
            : todas.Where(u => unidadesPermitidas.Contains(u.Id)).ToList();
        var unidadesDto = visibles.Select(u => new DashboardUnidadDto(u.Id, u.Nombre)).ToList();

        var generadoEn = DateTime.UtcNow;
        var hoy = generadoEn.Date;

        // Filtro efectivo de las métricas: la unidad elegida, o las permitidas (null = todas).
        IReadOnlyCollection<Guid>? filtro = unidadId.HasValue
            ? new[] { unidadId.Value }
            : unidadesPermitidas;

        // Dueño sin unidades asignadas (colección vacía, distinto de null): métricas en cero (E3).
        if (filtro is not null && filtro.Count == 0)
            return ConstruirVacio(generadoEn, hoy, unidadesDto);

        // Unidades sobre las que se desglosan socios por sede.
        var unidadesDesglose = unidadId.HasValue
            ? visibles.Where(u => u.Id == unidadId.Value).ToList()
            : visibles;

        var totalSociosActivos = await _socioRepository.CountActivosAsync(filtro);
        var porUnidad = new List<SociosPorUnidadDto>();
        foreach (var unidad in unidadesDesglose)
        {
            var cantidad = await _socioRepository.CountActivosByUnidadAsync(unidad.Id);
            porUnidad.Add(new SociosPorUnidadDto(unidad.Id, unidad.Nombre, cantidad));
        }

        var cuotas = new CuotasResumenDto(
            ProximasAVencer: await _cuotaRepository.CountPendientesPorVencerAsync(hoy, hoy.AddDays(DiasVentanaProximasAVencer), filtro),
            Vencidas: await _cuotaRepository.CountPendientesVencidasAsync(hoy, filtro),
            PagadasMes: await _cuotaRepository.CountPagadasDelMesAsync(hoy.Year, hoy.Month, filtro));

        var clasesDelDia = await ConstruirClasesDelDiaAsync(generadoEn, filtro);
        var recientes = (await _inscripcionClaseRepository.GetRecientesAsync(CantidadInscripcionesRecientes, filtro))
            .Select(i => new InscripcionRecienteDto(
                Socio: $"{i.Socio.Nombre} {i.Socio.Apellido}".Trim(),
                Clase: i.HorarioClase.Clase.Nombre,
                Unidad: i.HorarioClase.Clase.Unidad.Nombre,
                Fecha: i.FechaInscripcion))
            .ToList();

        var desdeSerie = hoy.AddDays(-(DiasSerieInscripciones - 1));
        var conteoPorDia = await _inscripcionClaseRepository.GetConteoActivasPorDiaAsync(desdeSerie, hoy, filtro);

        var grafica = new GraficaDto(
            SociosPorSede: porUnidad.Select(p => new SedeCantidadDto(p.UnidadNombre, p.Cantidad)).ToList(),
            CuotasPorEstado: ConstruirCuotasPorEstado(cuotas),
            InscripcionesUltimos7Dias: ConstruirSerieDiaria(desdeSerie, conteoPorDia));

        return new DashboardDto(
            GeneradoEn: generadoEn,
            Unidades: unidadesDto,
            SociosActivos: new SociosActivosDto(totalSociosActivos, porUnidad),
            Cuotas: cuotas,
            ClasesDelDia: clasesDelDia,
            InscripcionesRecientes: recientes,
            Grafica: grafica);
    }

    private async Task<IReadOnlyList<ClaseDelDiaDto>> ConstruirClasesDelDiaAsync(DateTime ahoraUtc, IReadOnlyCollection<Guid>? filtro)
    {
        var horarios = (await _horarioClaseRepository.GetByDiaAsync(MapDiaSemana(ahoraUtc.DayOfWeek), filtro)).ToList();
        if (horarios.Count == 0)
            return Array.Empty<ClaseDelDiaDto>();

        var inscriptos = await _inscripcionClaseRepository.GetConteoActivasPorHorariosAsync(horarios.Select(h => h.Id));

        return horarios
            .Select(h => new ClaseDelDiaDto(
                Clase: h.Clase.Nombre,
                Unidad: h.Clase.Unidad.Nombre,
                HoraInicio: h.HoraInicio.ToString("HH:mm"),
                HoraFin: h.HoraFin.ToString("HH:mm"),
                Cupo: h.Clase.CapacidadMaxima,
                Inscriptos: inscriptos.GetValueOrDefault(h.Id)))
            .ToList();
    }

    private static DashboardDto ConstruirVacio(DateTime generadoEn, DateTime hoy, IReadOnlyList<DashboardUnidadDto> unidades)
    {
        var cuotas = new CuotasResumenDto(0, 0, 0);
        return new DashboardDto(
            GeneradoEn: generadoEn,
            Unidades: unidades,
            SociosActivos: new SociosActivosDto(0, Array.Empty<SociosPorUnidadDto>()),
            Cuotas: cuotas,
            ClasesDelDia: Array.Empty<ClaseDelDiaDto>(),
            InscripcionesRecientes: Array.Empty<InscripcionRecienteDto>(),
            Grafica: new GraficaDto(
                SociosPorSede: Array.Empty<SedeCantidadDto>(),
                CuotasPorEstado: ConstruirCuotasPorEstado(cuotas),
                InscripcionesUltimos7Dias: ConstruirSerieDiaria(
                    hoy.AddDays(-(DiasSerieInscripciones - 1)),
                    new Dictionary<DateTime, int>())));
    }

    private static IReadOnlyList<EstadoCantidadDto> ConstruirCuotasPorEstado(CuotasResumenDto cuotas) =>
        new[]
        {
            new EstadoCantidadDto("Próximas a vencer", cuotas.ProximasAVencer),
            new EstadoCantidadDto("Vencidas", cuotas.Vencidas),
            new EstadoCantidadDto("Pagadas del mes", cuotas.PagadasMes),
        };

    /// <summary>Serie cronológica de 7 días con ceros en los días sin datos.</summary>
    private static IReadOnlyList<FechaCantidadDto> ConstruirSerieDiaria(DateTime desde, IReadOnlyDictionary<DateTime, int> conteoPorDia) =>
        Enumerable.Range(0, DiasSerieInscripciones)
            .Select(offset => desde.AddDays(offset))
            .Select(dia => new FechaCantidadDto(dia.ToString("yyyy-MM-dd"), conteoPorDia.GetValueOrDefault(dia)))
            .ToList();

    /// <summary>Mapea DayOfWeek (Sunday=0) al enum DiaSemana de los horarios (Lunes=1..Domingo=7).</summary>
    private static DiaSemana MapDiaSemana(DayOfWeek dia) =>
        dia == DayOfWeek.Sunday ? DiaSemana.Domingo : (DiaSemana)(int)dia;
}
