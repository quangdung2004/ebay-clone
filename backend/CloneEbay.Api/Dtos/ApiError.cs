namespace CloneEbay.Api.Dtos;

public record ApiError(
    string Code,
    string Message, 
    string? CorrelationId = null
    );
