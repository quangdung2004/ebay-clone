namespace CloneEbay.Domain.Exceptions;

public sealed class ConflictException : AppException
{
    public ConflictException(string message = "Conflict", string code = "CONFLICT")
        : base(code, message, 409) { }
}