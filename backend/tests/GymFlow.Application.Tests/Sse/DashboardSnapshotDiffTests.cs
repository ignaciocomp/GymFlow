using GymFlow.API.Sse;

namespace GymFlow.Application.Tests.Sse;

/// <summary>
/// Helper puro del stream SSE del dashboard (RF-18): decide si el snapshot debe emitirse.
/// Ignora generadoEn (cambia en cada recálculo aunque las métricas no cambien) y SIEMPRE
/// emite el primer snapshot (anterior null).
/// </summary>
public class DashboardSnapshotDiffTests
{
    [Fact]
    public void HaCambiado_SinSnapshotAnterior_SiempreTrue()
    {
        Assert.True(DashboardSnapshotDiff.HaCambiado(null, """{"generadoEn":"2026-07-04T10:00:00Z","total":1}"""));
    }

    [Fact]
    public void HaCambiado_MismasMetricasDistintoGeneradoEn_False()
    {
        var anterior = """{"generadoEn":"2026-07-04T10:00:00Z","sociosActivos":{"total":30}}""";
        var actual = """{"generadoEn":"2026-07-04T10:00:10Z","sociosActivos":{"total":30}}""";

        Assert.False(DashboardSnapshotDiff.HaCambiado(anterior, actual));
    }

    [Fact]
    public void HaCambiado_MetricasDistintas_True()
    {
        var anterior = """{"generadoEn":"2026-07-04T10:00:00Z","sociosActivos":{"total":30}}""";
        var actual = """{"generadoEn":"2026-07-04T10:00:10Z","sociosActivos":{"total":31}}""";

        Assert.True(DashboardSnapshotDiff.HaCambiado(anterior, actual));
    }

    [Fact]
    public void HaCambiado_JsonIdentico_False()
    {
        var json = """{"generadoEn":"2026-07-04T10:00:00Z","sociosActivos":{"total":30}}""";

        Assert.False(DashboardSnapshotDiff.HaCambiado(json, json));
    }

    [Fact]
    public void HaCambiado_TextoNoJson_ComparaComoTextoPlano()
    {
        Assert.False(DashboardSnapshotDiff.HaCambiado("no-json", "no-json"));
        Assert.True(DashboardSnapshotDiff.HaCambiado("no-json", "otro-texto"));
    }
}
