using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloneEbay.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CloneEbay.Infrastructure.Auth;

public class JwtService
{
    private readonly JwtOptions _opt;

    public JwtService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value;
    }

    public string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.email ?? ""),
            new(ClaimTypes.NameIdentifier, user.id.ToString()),
            new(ClaimTypes.Name, user.username ?? ""),
            new(ClaimTypes.Role, user.role ?? "USER")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpiryUtc(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var exp = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Exp).Value;
        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime;
    }
}
