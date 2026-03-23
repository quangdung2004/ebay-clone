namespace CloneEbay.Application.Common.Interfaces;

public interface IGeocodingService
{
    Task<(decimal latitude, decimal longitude)?> GeocodeLocationAsync(string locationText, CancellationToken ct = default);
}
