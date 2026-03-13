namespace CloneEbay.Application.Auth;

public interface ITokenBlacklistService
{
    Task BlacklistAsync(string token, DateTime expiryUtc);
    Task<bool> IsBlacklistedAsync(string token);
}
