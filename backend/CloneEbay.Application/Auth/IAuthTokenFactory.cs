namespace CloneEbay.Application.Auth;

public interface IAuthTokenFactory
{
    string GeneratePlainToken();
    string HashToken(string input);
}