using System.Diagnostics;

namespace MusicData.Infrastructure.Telemetry;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("MusicDataApi");
}