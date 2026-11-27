using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MusicData.Shared.Telemetry;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("MusicDataApi");

    private static readonly Meter Meter = new("MusicDataApi");

    /// <summary>Nombre de requêtes par entité (artist, album, lyrics) et résultat (cache, external, not_found).</summary>
    public static readonly Counter<long> Requests =
        Meter.CreateCounter<long>("musicdata.requests", "requests", "Number of API requests");

    /// <summary>Nombre d'appels aux services externes par service (lastfm, musicbrainz, fanart, etc.).</summary>
    public static readonly Counter<long> ExternalCalls =
        Meter.CreateCounter<long>("musicdata.external_calls", "calls", "Number of external service calls");

    /// <summary>Durée de traitement d'une requête par entité et résultat (cache, external, partial, not_found).</summary>
    public static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("musicdata.request.duration", "ms", "Duration of API requests handled");

    /// <summary>Classe une exception d'appel externe : timeout, rate_limited ou error.</summary>
    public static string ClassifyOutcome(Exception ex) => ex switch
    {
        TimeoutException => "timeout",
        TaskCanceledException => "timeout",
        HttpRequestException { StatusCode: System.Net.HttpStatusCode.TooManyRequests } => "rate_limited",
        _ => "error"
    };
}
