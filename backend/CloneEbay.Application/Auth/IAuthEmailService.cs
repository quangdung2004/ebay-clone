using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Auth;

public interface IAuthEmailService
{
    Task SendVerifyEmailAsync(User user, string plainToken, CancellationToken ct);
    Task SendResetPasswordEmailAsync(User user, string plainToken, CancellationToken ct);
}