using CloneEbay.Contracts.Auth;
using Microsoft.AspNetCore.Http;

namespace CloneEbay.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);

    Task<AuthResponse> LoginAsync(LoginRequest request, HttpContext context, CancellationToken ct);

    Task<RefreshResponse> RefreshAsync(HttpContext context, CancellationToken ct);

    Task LogoutAsync(HttpContext context, CancellationToken ct);

    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);

    Task<MeResponse> GetMeAsync(int userId, CancellationToken ct);
}