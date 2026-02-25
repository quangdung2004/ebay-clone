namespace CloneEbay.Api.Dtos;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public T? Data { get; init; }
    public string? CorrelationId { get; init; }

    public static ApiResponse<T> Ok(
        T data,
        string message = "Success",
        string code = "SUCCESS",
        string? correlationId = null)
        => new()
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data,
            CorrelationId = correlationId
        };

    public static ApiResponse<T> Fail(
        string message,
        string code = "ERROR",
        string? correlationId = null)
        => new()
        {
            Success = false,
            Code = code,
            Message = message,
            Data = default,
            CorrelationId = correlationId
        };
}
