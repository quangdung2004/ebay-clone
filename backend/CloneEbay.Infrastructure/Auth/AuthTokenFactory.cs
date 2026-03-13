using System.Security.Cryptography;
using System.Text;
using CloneEbay.Application.Auth;

namespace CloneEbay.Infrastructure.Auth;

public sealed class AuthTokenFactory : IAuthTokenFactory
{
    public string GeneratePlainToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}