using CloneEbay.Application.Auth;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CloneEbay.Api.Controllers;

[EnableRateLimiting("auth")]
[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<ApiResponse<AuthResponse>> Register(RegisterRequest req, CancellationToken ct)
        => Success(await _auth.RegisterAsync(req, ct), "Register successfully", "AUTH_REGISTER_SUCCESS");

    [HttpPost("login")]
    public async Task<ApiResponse<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
        => Success(await _auth.LoginAsync(req, HttpContext, ct), "Login successfully", "AUTH_LOGIN_SUCCESS");

    // Refresh access token by refreshToken cookie (rotation)
    [HttpPost("refresh")]
    public async Task<ApiResponse<RefreshResponse>> Refresh(CancellationToken ct)
        => Success(await _auth.RefreshAsync(HttpContext, ct), "Refresh successfully", "AUTH_REFRESH_SUCCESS");

    [Authorize]
    [HttpPost("logout")]
    public async Task<ApiResponse<object>> Logout(CancellationToken ct)
    {
        await _auth.LogoutAsync(HttpContext, ct);
        return Success("Logout successfully", "AUTH_LOGOUT_SUCCESS");
    }

    // Verify email by token (token comes from email link)
    [HttpPost("verify-email")]
    public async Task<ApiResponse<object>> VerifyEmail(VerifyEmailRequest req, CancellationToken ct)
    {
        await _auth.VerifyEmailAsync(req, ct);
        return Success("Verify email successfully", "AUTH_VERIFY_EMAIL_SUCCESS");
    }

    // Always return OK (do not leak whether email exists)
    [HttpPost("forgot-password")]
    public async Task<ApiResponse<object>> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(req, ct);
        return Success("If email exists, reset link has been sent", "AUTH_FORGOT_PASSWORD_OK");
    }

    [HttpPost("reset-password")]
    public async Task<ApiResponse<object>> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(req, ct);
        return Success("Reset password successfully", "AUTH_RESET_PASSWORD_SUCCESS");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ApiResponse<MeResponse>> Me(CancellationToken ct)
        => Success(await _auth.GetMeAsync(CurrentUserId, ct), "Get profile successfully", "AUTH_ME_SUCCESS");
}