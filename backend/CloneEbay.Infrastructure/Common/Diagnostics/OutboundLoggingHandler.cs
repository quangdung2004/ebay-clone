using System.Diagnostics;
using CloneEbay.Application.Common.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CloneEbay.Infrastructure.Common.Diagnostics;

public sealed class OutboundLoggingHandler : DelegatingHandler
{
    private readonly ILogger<OutboundLoggingHandler> _logger;
    private readonly ITransactionContextAccessor _transactionContext;

    public OutboundLoggingHandler(
        ILogger<OutboundLoggingHandler> logger,
        ITransactionContextAccessor transactionContext)
    {
        _logger = logger;
        _transactionContext = transactionContext;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var cid = _transactionContext.CorrelationId;
        var tx = _transactionContext.TransactionId;

        request.Headers.Remove("X-Correlation-Id");
        request.Headers.Remove("X-Transaction-Id");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", cid);
        request.Headers.TryAddWithoutValidation("X-Transaction-Id", tx);

        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Outgoing HTTP started | cid={cid} | tx={tx} | method={method} | uri={uri}",
            cid,
            tx,
            request.Method.Method,
            request.RequestUri?.ToString());

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "Outgoing HTTP finished | cid={cid} | tx={tx} | method={method} | uri={uri} | status={status} | elapsedMs={elapsed}",
                cid,
                tx,
                request.Method.Method,
                request.RequestUri?.ToString(),
                (int)response.StatusCode,
                sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(ex,
                "Outgoing HTTP failed | cid={cid} | tx={tx} | method={method} | uri={uri} | elapsedMs={elapsed}",
                cid,
                tx,
                request.Method.Method,
                request.RequestUri?.ToString(),
                sw.ElapsedMilliseconds);

            throw;
        }
    }
}