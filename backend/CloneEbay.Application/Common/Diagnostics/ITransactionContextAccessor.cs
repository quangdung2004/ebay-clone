namespace CloneEbay.Application.Common.Diagnostics;

public interface ITransactionContextAccessor
{
    string CorrelationId { get; }
    string TransactionId { get; }
}