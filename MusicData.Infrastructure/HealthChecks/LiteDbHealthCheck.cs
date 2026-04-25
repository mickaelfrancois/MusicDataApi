using LiteDB;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MusicData.Infrastructure.HealthChecks;

internal sealed class LiteDbHealthCheck(ILiteDatabase database) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = database.GetCollectionNames().Any();
            return Task.FromResult(HealthCheckResult.Healthy("LiteDB is reachable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("LiteDB is unreachable.", ex));
        }
    }
}
