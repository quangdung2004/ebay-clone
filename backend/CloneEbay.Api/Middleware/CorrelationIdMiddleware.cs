namespace CloneEbay.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationHeaderName = "X-Correlation-Id";
    private const string TransactionHeaderName = "X-Transaction-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        var transactionId = context.Request.Headers[TransactionHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            transactionId = Guid.NewGuid().ToString("N");
        }

        context.Items[CorrelationHeaderName] = correlationId;
        context.Items[TransactionHeaderName] = transactionId;

        context.Response.Headers[CorrelationHeaderName] = correlationId;
        context.Response.Headers[TransactionHeaderName] = transactionId;

        await _next(context);
    }
}