using MemoAna.Infrastructure.Identity.Options;
using MemoAna.Infrastructure.Identity.Services;
using Microsoft.Extensions.Logging;

namespace MemoAna.UnitTests.Identity;

/// <summary>Tests supporting Identity infrastructure branches.</summary>
public sealed class IdentityInfrastructureBranchTests
{
    [Fact]
    public async Task LoggingEmailSender_LogsBothMessageKinds()
    {
        using ILoggerFactory factory = LoggerFactory.Create(
            builder => builder.AddDebug());
        ILogger<LoggingIdentityEmailSender> logger =
            factory.CreateLogger<LoggingIdentityEmailSender>();
        LoggingIdentityEmailSender sender = new(logger);

        await sender.SendConfirmationAsync(
            "user@example.com", "/confirm",
            CancellationToken.None);
        await sender.SendPasswordResetAsync(
            "user@example.com", "/reset",
            CancellationToken.None);
    }

    [Fact]
    public void JwtOptions_DefaultsAreConfigured()
    {
        JwtOptions options =
            new()
            {
                Key = "01234567890123456789012345678901"
            };

        Assert.Equal("MemoAna", options.Issuer);
        Assert.Equal("MemoAna", options.Audience);
        Assert.Equal(TimeSpan.FromMinutes(15),
            options.AccessTokenLifetime);
        Assert.Equal(TimeSpan.FromDays(14),
            options.RefreshTokenLifetime);
    }
}
