using CloneEbay.Application.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Auth;

public sealed class AuthCookieService : IAuthCookieService
{
    private readonly AuthOptions _options;

    public AuthCookieService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public void SetRefreshTokenCookie(HttpContext context, string refreshToken, DateTime expiresUtc)
    {
        context.Response.Cookies.Append(AuthConstants.RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = _options.RefreshCookieSecure,
            SameSite = SameSiteMode.Lax,
            Expires = expiresUtc,
            Path = _options.RefreshCookiePath
        });
    }

    public void ClearRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(AuthConstants.RefreshCookieName, new CookieOptions
        {
            Path = _options.RefreshCookiePath,
            Secure = _options.RefreshCookieSecure,
            SameSite = SameSiteMode.Lax
        });
    }

    public bool TryGetRefreshToken(HttpContext context, out string? refreshToken)
    {
        var found = context.Request.Cookies.TryGetValue(AuthConstants.RefreshCookieName, out var value);
        refreshToken = found ? value : null;
        return found && !string.IsNullOrWhiteSpace(refreshToken);
    }
}