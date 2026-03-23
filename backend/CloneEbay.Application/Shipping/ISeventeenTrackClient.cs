using CloneEbay.Contracts.Shipping;

namespace CloneEbay.Application.Shipping;

public interface ISeventeenTrackClient
{
    Task<Register17TrackResultDto> RegisterTrackingAsync(Register17TrackRequest request, CancellationToken ct);
}