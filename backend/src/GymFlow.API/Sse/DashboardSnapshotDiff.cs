using System.Text.Json;
using System.Text.Json.Nodes;

namespace GymFlow.API.Sse;

/// <summary>
/// Helper puro del stream SSE del dashboard (RF-18): decide si un snapshot serializado debe
/// emitirse comparándolo con el último enviado. Ignora la propiedad raíz <c>generadoEn</c>
/// (timestamp que cambia en cada recálculo aunque las métricas no cambien); sin ella, dos
/// snapshots iguales producen heartbeat en vez de re-emisión. El primer snapshot
/// (<paramref name="jsonAnterior"/> null) siempre se emite.
/// </summary>
public static class DashboardSnapshotDiff
{
    public static bool HaCambiado(string? jsonAnterior, string jsonActual)
    {
        if (jsonAnterior is null)
            return true;

        return !string.Equals(Normalizar(jsonAnterior), Normalizar(jsonActual), StringComparison.Ordinal);
    }

    private static string Normalizar(string json)
    {
        try
        {
            if (JsonNode.Parse(json) is not JsonObject objeto)
                return json;

            var claveGeneradoEn = objeto
                .Select(p => p.Key)
                .FirstOrDefault(k => string.Equals(k, "generadoEn", StringComparison.OrdinalIgnoreCase));

            if (claveGeneradoEn is not null)
                objeto.Remove(claveGeneradoEn);

            return objeto.ToJsonString();
        }
        catch (JsonException)
        {
            return json; // no parseable: comparar como texto plano
        }
    }
}
