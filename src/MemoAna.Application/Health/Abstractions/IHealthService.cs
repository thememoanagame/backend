using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemoAna.Application.Health.Abstractions;

public interface IHealthService
{
    Task<HealthReport> GetApiDiskUsageAsync(CancellationToken cancellationToken);
    Task<HealthReport> GetHealthCheckAsync(CancellationToken cancellationToken);
    Task<HealthReport> GetHostInfoAsync(CancellationToken cancellationToken);
    Task<HealthReport> PerformDbReadinessCheckAsync(CancellationToken cancellationToken);
}
