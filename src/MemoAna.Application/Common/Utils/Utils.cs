using System.Diagnostics;

namespace MemoAna.Application.Common.Utils;

public static class Utils
{
    private static string Environment = string.Empty;
    public static void SetEnvironment(string environment) => Environment = environment;

    public static string GetEnvironment() => Environment;

    public static string GetUpTime()
    {
        var span = DateTime.UtcNow - Process.GetCurrentProcess().StartTime;
        if (span.Days > 0)
            return $"{span.Days}d {span.Hours:D2}h:{span.Minutes:D2}m:{span.Seconds:D2}s";

        return $"{span.Hours:D2}h:{span.Minutes:D2}m:{span.Seconds:D2}s";
    }
}
