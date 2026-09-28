using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoAna.Infrastructure.Identity.Services;

/// <summary>Authenticates the protected seed endpoint using deployment-provided Basic credentials.</summary>
public sealed class SeedBasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? configuredUser = Environment.GetEnvironmentVariable("SEEDU");
        string? configuredPassword = Environment.GetEnvironmentVariable("SEEDP");

        if (string.IsNullOrEmpty(configuredUser) || string.IsNullOrEmpty(configuredPassword))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Seed credentials are not configured."));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var values)
            || !AuthenticationHeaderValue.TryParse(values.ToString(), out AuthenticationHeaderValue? header)
            || !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(header.Parameter))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
            int separator = decoded.IndexOf(':');

            if (separator < 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication credentials."));
            }

            string user = decoded[..separator];
            string password = decoded[(separator + 1)..];

            bool validUser = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user),
                Encoding.UTF8.GetBytes(configuredUser));

            bool validPassword = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(password),
                Encoding.UTF8.GetBytes(configuredPassword));

            if (!validUser || !validPassword)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication credentials."));
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, configuredUser)],
                Scheme.Name);

            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(identity),
                        Scheme.Name)));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication credentials."));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = $"Basic realm=\"MemoAna Seed\", charset=\"UTF-8\"";
        return Task.CompletedTask;
    }
}
