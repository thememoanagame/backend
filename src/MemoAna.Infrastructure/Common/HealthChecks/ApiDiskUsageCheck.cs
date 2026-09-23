using MemoAna.Application.Common.Utils;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemoAna.Infrastructure.Common.HealthChecks;

public sealed class ApiDiskUsageCheck : IHealthCheck
{
    private readonly DriveInfo drive = new(Path.GetPathRoot(Environment.CurrentDirectory)!);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                {
                    "ApiDiskUsage",
                    new
                    {
                        TotalSize =  ByteFormatter.Normalize(drive.TotalSize),
                        UsedSize = ByteFormatter.Normalize(drive.TotalSize - drive.TotalFreeSpace) ,
                        FreeSize = ByteFormatter.Normalize(drive.AvailableFreeSpace)
                    }
                }
            };

            return Task.FromResult(
                HealthCheckResult.Healthy("API disk is healthy", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("The API disk is unavailable", ex));
        }
    }

}
