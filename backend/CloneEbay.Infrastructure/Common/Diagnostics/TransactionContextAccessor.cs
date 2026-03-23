using CloneEbay.Application.Common.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace CloneEbay.Infrastructure.Common.Diagnostics;

public sealed class TransactionContextAccessor : ITransactionContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TransactionContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Items["X-Correlation-Id"]?.ToString()
                   ?? httpContext?.TraceIdentifier
                   ?? "no-correlation-id";
        }
    }

    public string TransactionId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Items["X-Transaction-Id"]?.ToString()
                   ?? CorrelationId;
        }
    }
}