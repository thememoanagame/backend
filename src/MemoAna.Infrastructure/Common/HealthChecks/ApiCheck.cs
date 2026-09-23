using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace MemoAna.Infrastructure.Common.HealthChecks;

public sealed class ApiCheck : IHealthCheck
{
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var uptime = DateTime.UtcNow - _currentProcess.StartTime.ToUniversalTime();
            var memoryUsage = _currentProcess.WorkingSet64; // bytes
            var cpuTime = _currentProcess.TotalProcessorTime;

            // Critérios simples de saúde 
            if (memoryUsage > 500_000_000) // > 5000MB
            {
                return Task.FromResult(HealthCheckResult.Degraded($"High memory usage: {memoryUsage / 1024 / 1024} MB"));
            }

            // Se chegou até aqui, consideramos saudável
            var data = new Dictionary<string, object>
            {
                {
                    "ResourceUsage",
                    new
                    {
                        Uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
                        MemoryUsageMB = memoryUsage / 1024 / 1024,
                        CpuTime = cpuTime.ToString()
                    }
                }
            };

            return Task.FromResult(HealthCheckResult.Healthy("API process is healthy", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("The API is unreachable", ex));
        }
    }

}
