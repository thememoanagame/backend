using MemoAna.Application.Identity.Abstractions;

namespace MemoAna.UnitTests.Common.Helpers;

internal sealed class CapturingEmailSender : IIdentityEmailSender
{
    public List<string> ConfirmationLinks { get; } = [];
    public List<string> PasswordResetLinks { get; } = [];

    public Task SendConfirmationAsync(string email, string confirmationLink, CancellationToken cancellationToken)
    {
        ConfirmationLinks.Add(confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken)
    {
        PasswordResetLinks.Add(resetLink);
        return Task.CompletedTask;
    }
}
