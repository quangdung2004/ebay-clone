namespace CloneEbay.Domain.Exceptions;

public sealed class ValidationException : AppException
{
    public ValidationException(string message, string code = "VALIDATION_ERROR")
        : base(code, message, 400) { }
}