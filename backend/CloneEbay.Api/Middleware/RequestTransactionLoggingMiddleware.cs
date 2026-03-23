using System.Diagnostics;

namespace CloneEbay.Api.Middleware;

public class RequestTransactionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTransactionLoggingMiddleware> _logger;

    public RequestTransactionLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestTransactionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId =
            context.Items["X-Correlation-Id"]?.ToString()
            ?? context.TraceIdentifier;

        var transactionId =
            context.Items["X-Transaction-Id"]?.ToString()
            ?? correlationId;

        var sw = Stopwatch.StartNew();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TransactionId"] = transactionId
        }))
        {
            _logger.LogInformation(
                "HTTP request started | cid={cid} | tx={tx} | method={method} | path={path} | query={query} | ip={ip}",
                correlationId,
                transactionId,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "",
                context.Connection.RemoteIpAddress?.ToString());

            try
            {
                await _next(context);

                sw.Stop();

                _logger.LogInformation(
                    "HTTP request finished | cid={cid} | tx={tx} | method={method} | path={path} | status={status} | elapsedMs={elapsed}",
                    correlationId,
                    transactionId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds);
            }
            catch
            {
                sw.Stop();

                _logger.LogError(
                    "HTTP request failed | cid={cid} | tx={tx} | method={method} | path={path} | elapsedMs={elapsed}",
                    correlationId,
                    transactionId,
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds);

                throw;
            }
        }
    }
}