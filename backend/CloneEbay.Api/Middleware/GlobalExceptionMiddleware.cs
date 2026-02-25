using System.Net;
using System.Text.Json;
using CloneEbay.Api.Dtos;
using CloneEbay.Api.Services.Auth;

namespace CloneEbay.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId =
                context.Items["X-Correlation-Id"]?.ToString()
                ?? context.TraceIdentifier;

            _logger.LogError(ex,
                "Unhandled error | cid={cid} | path={path}",
                correlationId,
                context.Request.Path);

            await WriteErrorAsync(context, ex, correlationId);
        }
    }

    private static Task WriteErrorAsync(
    HttpContext context,
    Exception ex,
    string correlationId)
    {
        int statusCode;
        string code;
        string message;

        if (ex is AuthException authEx)
        {
            if (authEx.Unauthorized)
            {
                statusCode = (int)HttpStatusCode.Unauthorized;
                code = "AUTH_UNAUTHORIZED";
                message = authEx.Message;
            }
            else
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                code = "AUTH_ERROR";
                message = authEx.Message;
            }
        }
        else if (ex is BadHttpRequestException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            code = "BAD_REQUEST";
            message = ex.Message;
        }
        else
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            code = "INTERNAL_ERROR";
            message = "Something went wrong";
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(
            message: message,
            code: code,
            correlationId: correlationId
        );

        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        );

        return context.Response.WriteAsync(json);
    }

}
