namespace CloneEbay.Domain.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden", string code = "FORBIDDEN")
        : base(code, message, 403) { }
}