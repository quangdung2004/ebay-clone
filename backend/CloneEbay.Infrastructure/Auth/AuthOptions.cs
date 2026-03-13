namespace CloneEbay.Infrastructure.Auth;

public sealed class AuthOptions
{
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
    public int RefreshTokenDaysRememberMe { get; set; } = 30;
    public int VerifyEmailMinutes { get; set; } = 1440;
    public int ResetPasswordMinutes { get; set; } = 60;
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    public bool RefreshCookieSecure { get; set; } = false;
    public string RefreshCookiePath { get; set; } = "/api/auth";
}