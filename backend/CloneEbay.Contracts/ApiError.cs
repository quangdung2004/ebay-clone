namespace CloneEbay.Contracts;

public record ApiError(
    string Code,
    string Message,
    string? CorrelationId = null,
    Dictionary<string, string[]>? Errors = null
);