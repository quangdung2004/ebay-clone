using System.Net;
using System.Text.Json;
using CloneEbay.Contracts;
using CloneEbay.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

            var transactionId =
                context.Items["X-Transaction-Id"]?.ToString()
                ?? correlationId;

            _logger.LogError(ex,
                "Unhandled error | cid={cid} | tx={tx} | path={path} | method={method}",
                correlationId,
                transactionId,
                context.Request.Path,
                context.Request.Method);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "Response already started | cid={cid} | tx={tx}",
                    correlationId,
                    transactionId);
                throw;
            }

            await WriteErrorAsync(context, ex, correlationId, transactionId);
        }
    }

    private static Task WriteErrorAsync(
        HttpContext context,
        Exception ex,
        string correlationId,
        string transactionId)
    {
        int statusCode;
        string code;
        string message;

        if (ex is AppException appEx)
        {
            statusCode = appEx.StatusCode;
            code = appEx.Code;
            message = appEx.Message;
        }
        else if (ex is AuthException authEx)
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
        else if (ex is DbUpdateException)
        {
            statusCode = (int)HttpStatusCode.Conflict;
            code = "DB_CONFLICT";
            message = "Database conflict.";
        }
        else
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            code = "INTERNAL_ERROR";
            message = "Something went wrong.";
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Response.Headers["X-Transaction-Id"] = transactionId;

        var response = ApiResponse<object>.Fail(
            message: message,
            code: code,
            correlationId: correlationId
        );

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOpts));
    }
}