using System.Security.Claims;
using CloneEbay.Api.Dtos;
using CloneEbay.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Api.Services.Auth;

public class AuthService : IAuthService
{

    private readonly CloneEbayDbContext _dbContext;
    private readonly JwtService _jwt;
    private readonly ITokenBlacklistService _blacklist;
    public AuthService(
        CloneEbayDbContext dbContext,
        JwtService jwt,
        ITokenBlacklistService blacklist
    )
    {
        _dbContext = dbContext;
        _jwt = jwt;
        _blacklist = blacklist;
    }
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.ToLowerInvariant();

        var user = await _dbContext.User
            .FirstOrDefaultAsync(u => u.email == email, ct);
        if (user == null)
        {
            throw new AuthException("Invalid email or password.", true);
        } 

        if(!BCrypt.Net.BCrypt.Verify(request.Password, user.password))
        {
            throw new AuthException("Invalid email or password.", true);
        }

        var token = _jwt.CreateToken(user);

        return ToAuthResponse(user, token);

    }

    public async Task LogoutAsync(HttpContext context, CancellationToken ct)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer "))
        {
            throw new AuthException("Invalid token.", true);
        }

        var token = authHeader["Bearer ".Length..].Trim();

        var expiry = _jwt.GetExpiryUtc(token);

        await _blacklist.BlacklistAsync(token, expiry);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.ToLowerInvariant();

        if (await _dbContext.User.AnyAsync(u => u.email == email, ct))
        {
            throw new AuthException("Email already exists.");
        }

        var user = CreateUser(request);

        _dbContext.User.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        var token = _jwt.CreateToken(user);

        return ToAuthResponse(user,token);

    }

    public async Task<MeResponse> GetMeAsync(int userId, CancellationToken ct)
    {
        var user = await _dbContext.User
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.id == userId, ct);

        if (user == null)
            throw new AuthException("User not found", unauthorized: true);

        return new MeResponse(
            Username: user.username ?? "",
            Email: user.email ?? "",
            Role: user.role ?? "USER",
            AvatarURL: user.avatarURL
        );
    }

    private static User CreateUser(RegisterRequest req)
    {
        return new User
        {
            username = req.Username.Trim(),
            email = req.Email.Trim().ToLowerInvariant(),
            password = BCrypt.Net.BCrypt.HashPassword(req.Password),
            role = "USER"
        };
    }

    public static AuthResponse ToAuthResponse(User user, string token)
    {
        return new AuthResponse(
            user.id,
            user.username ?? "",
            user.email ?? "",
            user.role ?? "USER",
            token
        );
    }
}
