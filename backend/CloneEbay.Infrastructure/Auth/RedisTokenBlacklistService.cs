using CloneEbay.Application.Auth;
using StackExchange.Redis;

namespace CloneEbay.Infrastructure.Auth;

public class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly IDatabase _db;

    public RedisTokenBlacklistService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task BlacklistAsync(string token, DateTime expiryUtc)
    {
        var ttl = expiryUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
            return;

        await _db.StringSetAsync(GetKey(token), "revoked", ttl);
    }

    public async Task<bool> IsBlacklistedAsync(string token)
    {
        return await _db.KeyExistsAsync(GetKey(token));
    }

    private static string GetKey(string token)
        => $"blacklist:{token}";
}
