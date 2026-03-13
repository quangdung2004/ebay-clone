namespace CloneEbay.Domain.Exceptions;

public class AuthException : Exception
{
    public bool Unauthorized { get; }

    public AuthException(string message, bool unauthorized = false)
        : base(message)
    {
        Unauthorized = unauthorized;
    }
}
