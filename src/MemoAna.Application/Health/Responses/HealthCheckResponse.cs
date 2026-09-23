using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace MemoAna.Application.Health.Responses;

public sealed record HealthCheckResponse(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> Entries, string Status, string Duration) 
{
    internal static HealthCheckResponse FromHealthReport(HealthReport healthReport)
    {
        // Converte cada entry para string
        var entries = healthReport.Entries.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Data);

        // Converte status agregado
        var status = healthReport.Status.ToString();

        // Formata duração total em hh:mm:ss
        string duration = new StringBuilder()
            .Append($"{(int)healthReport.TotalDuration.TotalHours:D2}:")
            .Append($"{healthReport.TotalDuration.Minutes:D2}:")
            .Append($"{healthReport.TotalDuration.Seconds:D2}:")
            .Append($"{healthReport.TotalDuration.Milliseconds} ")
            .ToString();

        return new HealthCheckResponse(entries, status, duration);

    }
}