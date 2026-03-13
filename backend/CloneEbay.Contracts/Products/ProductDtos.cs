using System.ComponentModel.DataAnnotations;
using CloneEbay.Contracts.Validation;

namespace CloneEbay.Contracts.Products;

public record ProductListItemDto(
    int id,
    string title,
    decimal price,
    string? thumbnailUrl,
    int? categoryId,
    string? categoryName,
    int? sellerId,
    string? sellerName,
    bool? isAuction,
    string? status,
    string? condition,
    bool inStock,
    int availableQuantity,
    int bidCount,
    decimal? currentBid,
    bool isEnded,
    DateTime? auctionEndTime,
    int viewCount
);

public record ProductDetailDto(
    int id,
    string title,
    string? description,
    decimal price,
    List<string> images,
    int? categoryId,
    string? categoryName,
    int? sellerId,
    string? sellerName,
    bool? isAuction,
    DateTime? auctionEndTime,
    string? status,
    string? condition,
    int availableQuantity,
    bool inStock,
    int bidCount,
    decimal? currentBid,
    bool isEnded,
    string? timeLeft,
    int viewCount
);

[UtcFutureDateIf(nameof(CreateProductRequest.isAuction), nameof(CreateProductRequest.auctionEndTime))]
public record CreateProductRequest(
    [param: RequiredTrimmed]
    [param: StringLength(150, MinimumLength = 3, ErrorMessage = ValidationMessages.StringLength)]
    string title,

    [param: StringLength(2000, ErrorMessage = ValidationMessages.MaxLength)]
    string? description,

    [param: Range(typeof(decimal), "0.01", "999999999", ErrorMessage = ValidationMessages.PositiveNumber)]
    decimal price,

    [param: Range(1, int.MaxValue, ErrorMessage = ValidationMessages.PositiveNumber)]
    int categoryId,

    bool isAuction,

    DateTime? auctionEndTime,

    [param: Range(0, int.MaxValue, ErrorMessage = ValidationMessages.NonNegativeNumber)]
    int quantity,

    [param: Required]
    [param: StringLength(30, ErrorMessage = ValidationMessages.MaxLength)]
    string condition
);

[UtcFutureDateIf(nameof(UpdateProductRequest.isAuction), nameof(UpdateProductRequest.auctionEndTime))]
public record UpdateProductRequest(
    [param: RequiredTrimmed]
    [param: StringLength(150, MinimumLength = 3, ErrorMessage = ValidationMessages.StringLength)]
    string title,

    [param: StringLength(2000, ErrorMessage = ValidationMessages.MaxLength)]
    string? description,

    [param: Range(typeof(decimal), "0.01", "999999999", ErrorMessage = ValidationMessages.PositiveNumber)]
    decimal price,

    [param: Range(1, int.MaxValue, ErrorMessage = ValidationMessages.PositiveNumber)]
    int categoryId,

    bool isAuction,

    DateTime? auctionEndTime,

    [param: Required]
    [param: StringLength(30, ErrorMessage = ValidationMessages.MaxLength)]
    string condition
);

public record UpdateInventoryRequest(
    [param: Range(0, int.MaxValue, ErrorMessage = ValidationMessages.NonNegativeNumber)]
    int quantity
);

public record UpdateProductStatusRequest(
    [param: Required]
    [param: StringLength(30, ErrorMessage = ValidationMessages.MaxLength)]
    string status
);

public record DeleteProductImageRequest(
    [param: Required]
    string imageUrl
);

public record PagedResponse<T>(IReadOnlyList<T> items, int page, int pageSize, int total);