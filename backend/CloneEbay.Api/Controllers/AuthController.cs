using System.Security.Claims;
using CloneEbay.Api.Dtos;
using CloneEbay.Api.Services.Auth;
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
        => Success(await _auth.LoginAsync(req, ct), "Login successfully", "AUTH_LOGIN_SUCCESS");

    [Authorize]
    [HttpPost("logout")]
    public async Task<ApiResponse<object>> Logout(CancellationToken ct)
    {
        await _auth.LogoutAsync(HttpContext, ct);
        return Success("Logout successfully", "AUTH_LOGOUT_SUCCESS");
    }


    [Authorize]
    [HttpGet("me")]
    public async Task<ApiResponse<MeResponse>> Me(CancellationToken ct)
    => Success(
        await _auth.GetMeAsync(CurrentUserId, ct),
        "Get profile successfully",
        "AUTH_ME_SUCCESS"
    );

}
