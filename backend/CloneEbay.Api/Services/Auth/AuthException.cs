namespace CloneEbay.Api.Services.Auth;

public class AuthException : Exception
{
    public bool Unauthorized { get; }

    public AuthException(string message, bool unauthorized = false)
        : base(message)
    {
        Unauthorized = unauthorized;
    }
}
