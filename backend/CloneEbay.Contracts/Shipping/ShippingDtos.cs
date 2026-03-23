namespace CloneEbay.Contracts.Shipping;

public sealed record CreateShipmentRequest(
    string carrier,
    string trackingNumber,
    DateTime? estimatedArrival,
    string? provider
);

public sealed record UpdateShipmentDeliveredRequest(
    DateTime? deliveredAt
);

public sealed record ShippingDetailDto(
    int id,
    int? orderId,
    string? carrier,
    string? trackingNumber,
    string? status,
    DateTime? estimatedArrival,
    DateTime? shippedAt,
    DateTime? deliveredAt,
    string? provider,
    string? providerTrackingId,
    DateTime? lastSyncedAt
);

public sealed record MockShipmentStatusRequest(
    string status,
    string? description,
    string? location,
    DateTime? eventTime
);

public sealed record Register17TrackRequest(
    string number,
    int? carrier,
    string? tag
);

public sealed record Register17TrackResultDto(
    string trackingNumber,
    bool success,
    string? message
);

public sealed class SeventeenTrackWebhookRequest
{
    public string? eventType { get; set; }
    public List<SeventeenTrackWebhookDataItem>? data { get; set; }
}

public sealed class SeventeenTrackWebhookDataItem
{
    public string? number { get; set; }
    public string? tag { get; set; }
    public SeventeenTrackTrackPayload? track { get; set; }
}

public sealed class SeventeenTrackTrackPayload
{
    public SeventeenTrackPackageStatus? package_status { get; set; }
    public List<SeventeenTrackOriginInfoItem>? origin_info { get; set; }
}

public sealed class SeventeenTrackPackageStatus
{
    public string? status { get; set; }
    public string? sub_status { get; set; }
}
public sealed class SeventeenTrackOriginInfoItem
{
    public string? description { get; set; }
    public string? status { get; set; }
    public string? sub_status { get; set; }
    public string? location { get; set; }
    public string? address { get; set; }
    public string? city { get; set; }
    public string? country { get; set; }
    public DateTime? event_time { get; set; }
}