using CloneEbay.Application.Auth;
using CloneEbay.Contracts.Auth;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly CloneEbayDbContext _dbContext;
    private readonly JwtService _jwt;
    private readonly ITokenBlacklistService _blacklist;
    private readonly IAuthTokenFactory _tokenFactory;
    private readonly IAuthCookieService _cookieService;
    private readonly IAuthEmailService _authEmailService;
    private readonly AuthOptions _options;

    public AuthService(
        CloneEbayDbContext dbContext,
        JwtService jwt,
        ITokenBlacklistService blacklist,
        IAuthTokenFactory tokenFactory,
        IAuthCookieService cookieService,
        IAuthEmailService authEmailService,
        IOptions<AuthOptions> options)
    {
        _dbContext = dbContext;
        _jwt = jwt;
        _blacklist = blacklist;
        _tokenFactory = tokenFactory;
        _cookieService = cookieService;
        _authEmailService = authEmailService;
        _options = options.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, HttpContext context, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _dbContext.User
            .FirstOrDefaultAsync(x => x.email == email, ct);

        EnsureValidLogin(user, request.Password);

        if (user!.emailVerified != true)
            throw new AuthException("Please verify your email first.", true);

        var now = DateTime.UtcNow;
        var refreshExpiresAt = now.AddDays(GetRefreshTokenDays(request.RememberMe));
        var refreshPlain = _tokenFactory.GeneratePlainToken();
        var refreshHash = _tokenFactory.HashToken(refreshPlain);

        _dbContext.RefreshToken.Add(new RefreshToken
        {
            userId = user.id,
            tokenHash = refreshHash,
            createdAt = now,
            expiresAt = refreshExpiresAt
        });

        await _dbContext.SaveChangesAsync(ct);

        _cookieService.SetRefreshTokenCookie(context, refreshPlain, refreshExpiresAt);

        return BuildAuthResponse(user);
    }

    public async Task<RefreshResponse> RefreshAsync(HttpContext context, CancellationToken ct)
    {
        if (!_cookieService.TryGetRefreshToken(context, out var refreshPlain))
            throw new AuthException("Missing refresh token.", true);

        var refreshHash = _tokenFactory.HashToken(refreshPlain!);

        var token = await _dbContext.RefreshToken
            .Include(x => x.user)
            .FirstOrDefaultAsync(x => x.tokenHash == refreshHash, ct);

        EnsureValidRefreshToken(token);

        var now = DateTime.UtcNow;
        var newPlain = _tokenFactory.GeneratePlainToken();
        var newHash = _tokenFactory.HashToken(newPlain);

        token!.revokedAt = now;
        token.replacedByTokenHash = newHash;

        _dbContext.RefreshToken.Add(new RefreshToken
        {
            userId = token.userId,
            tokenHash = newHash,
            createdAt = now,
            expiresAt = token.expiresAt
        });

        await _dbContext.SaveChangesAsync(ct);

        _cookieService.SetRefreshTokenCookie(context, newPlain, token.expiresAt);

        var accessToken = _jwt.CreateToken(token.user!);
        return new RefreshResponse(accessToken, _options.AccessTokenMinutes);
    }

    public async Task LogoutAsync(HttpContext context, CancellationToken ct)
    {
        var accessToken = ExtractBearerToken(context);
        var accessExpiry = _jwt.GetExpiryUtc(accessToken);

        await _blacklist.BlacklistAsync(accessToken, accessExpiry);

        if (_cookieService.TryGetRefreshToken(context, out var refreshPlain))
        {
            var refreshHash = _tokenFactory.HashToken(refreshPlain!);

            var refreshRow = await _dbContext.RefreshToken
                .FirstOrDefaultAsync(x => x.tokenHash == refreshHash, ct);

            if (refreshRow is not null && refreshRow.revokedAt is null)
            {
                refreshRow.revokedAt = DateTime.UtcNow;
                refreshRow.revokedByIp = context.Connection.RemoteIpAddress?.ToString();
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        _cookieService.ClearRefreshTokenCookie(context);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        var emailExists = await _dbContext.User
            .AnyAsync(x => x.email == email, ct);

        if (emailExists)
            throw new ValidationException("Email already exists.", "EMAIL_ALREADY_EXISTS");

        var user = new User
        {
            username = request.Username.Trim(),
            email = email,
            password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            role = AuthConstants.DefaultUserRole,
            emailVerified = false
        };

        _dbContext.User.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        var verifyPlain = _tokenFactory.GeneratePlainToken();
        var verifyHash = _tokenFactory.HashToken(verifyPlain);

        _dbContext.UserToken.Add(new UserToken
        {
            userId = user.id,
            type = AuthConstants.VerifyEmailTokenType,
            tokenHash = verifyHash,
            createdAt = now,
            expiresAt = now.AddMinutes(_options.VerifyEmailMinutes)
        });

        await _dbContext.SaveChangesAsync(ct);

        await _authEmailService.SendVerifyEmailAsync(user, verifyPlain, ct);

        return BuildAuthResponse(user);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        var token = await FindValidUserTokenAsync(
            request.Token,
            AuthConstants.VerifyEmailTokenType,
            ct);

        var user = await _dbContext.User
            .FirstAsync(x => x.id == token.userId, ct);

        if (user.emailVerified == true)
            return;

        var now = DateTime.UtcNow;
        user.emailVerified = true;
        user.emailVerifiedAt = now;
        token.usedAt = now;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _dbContext.User
            .FirstOrDefaultAsync(x => x.email == email, ct);

        if (user is null)
            return;

        var now = DateTime.UtcNow;
        var resetPlain = _tokenFactory.GeneratePlainToken();
        var resetHash = _tokenFactory.HashToken(resetPlain);

        _dbContext.UserToken.Add(new UserToken
        {
            userId = user.id,
            type = AuthConstants.ResetPasswordTokenType,
            tokenHash = resetHash,
            createdAt = now,
            expiresAt = now.AddMinutes(_options.ResetPasswordMinutes)
        });

        await _dbContext.SaveChangesAsync(ct);

        await _authEmailService.SendResetPasswordEmailAsync(user, resetPlain, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var token = await FindValidUserTokenAsync(
            request.Token,
            AuthConstants.ResetPasswordTokenType,
            ct);

        var user = await _dbContext.User
            .FirstAsync(x => x.id == token.userId, ct);

        user.password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        token.usedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<MeResponse> GetMeAsync(int userId, CancellationToken ct)
    {
        var user = await _dbContext.User
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.id == userId, ct);

        if (user is null)
            throw new AuthException("User not found", true);

        return new MeResponse(
            Id: user.id,
            Username: user.username ?? "",
            Email: user.email ?? "",
            Role: user.role ?? AuthConstants.DefaultUserRole,
            AvatarURL: user.avatarURL
        );
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var accessToken = _jwt.CreateToken(user);

        return new AuthResponse(
            user.id,
            user.username ?? "",
            user.email ?? "",
            user.role ?? AuthConstants.DefaultUserRole,
            accessToken,
            _options.AccessTokenMinutes
        );
    }

    private int GetRefreshTokenDays(bool rememberMe)
        => rememberMe ? _options.RefreshTokenDaysRememberMe : _options.RefreshTokenDays;

    private async Task<UserToken> FindValidUserTokenAsync(string plainToken, string tokenType, CancellationToken ct)
    {
        var tokenHash = _tokenFactory.HashToken(plainToken);

        var token = await _dbContext.UserToken
            .FirstOrDefaultAsync(x => x.tokenHash == tokenHash && x.type == tokenType, ct);

        if (token is null || token.usedAt is not null || token.expiresAt <= DateTime.UtcNow)
            throw new AuthException("Invalid or expired token.");

        return token;
    }

    private static void EnsureValidLogin(User? user, string password)
    {
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.password))
            throw new AuthException("Invalid email or password.", true);
    }

    private static void EnsureValidRefreshToken(RefreshToken? token)
    {
        if (token is null || token.revokedAt is not null || token.expiresAt <= DateTime.UtcNow)
            throw new AuthException("Invalid refresh token.", true);
    }

    private static string ExtractBearerToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            throw new AuthException("Invalid token.", true);

        return authHeader["Bearer ".Length..].Trim();
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}