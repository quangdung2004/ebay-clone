using System.Net;
using CloneEbay.Application.Auth;
using CloneEbay.Application.Notifications;
using CloneEbay.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Auth;

public sealed class AuthEmailService : IAuthEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly AuthOptions _options;

    public AuthEmailService(IEmailSender emailSender, IOptions<AuthOptions> options)
    {
        _emailSender = emailSender;
        _options = options.Value;
    }

    public Task SendVerifyEmailAsync(User user, string plainToken, CancellationToken ct)
    {
        var link = $"{_options.FrontendBaseUrl}/verify-email?token={WebUtility.UrlEncode(plainToken)}";
        var subject = "Verify your email";
        var body = $@"
<h3>Welcome to CloneEbay</h3>
<p>Please verify your email by clicking the link below:</p>
<p><a href=""{link}"">{link}</a></p>
<p>This link will expire in {_options.VerifyEmailMinutes} minutes.</p>";

        return _emailSender.SendAsync(user.email!, subject, body, ct);
    }

    public Task SendResetPasswordEmailAsync(User user, string plainToken, CancellationToken ct)
    {
        var link = $"{_options.FrontendBaseUrl}/reset-password?token={WebUtility.UrlEncode(plainToken)}";
        var subject = "Reset your password";
        var body = $@"
<h3>Reset password</h3>
<p>Click the link below to reset your password:</p>
<p><a href=""{link}"">{link}</a></p>
<p>This link will expire in {_options.ResetPasswordMinutes} minutes.</p>";

        return _emailSender.SendAsync(user.email!, subject, body, ct);
    }
}