using Microsoft.AspNetCore.Http;

namespace CloneEbay.Application.Auth;

public interface IAuthCookieService
{
    void SetRefreshTokenCookie(HttpContext context, string refreshToken, DateTime expiresUtc);
    void ClearRefreshTokenCookie(HttpContext context);
    bool TryGetRefreshToken(HttpContext context, out string? refreshToken);
}