
using System.Security.Claims;
using CloneEbay.Api.Dtos;

namespace CloneEbay.Api.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);

        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);

        Task LogoutAsync(HttpContext context, CancellationToken ct);

        Task<MeResponse> GetMeAsync(int userId, CancellationToken ct);


    }
}
