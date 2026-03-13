using System.ComponentModel.DataAnnotations;
using CloneEbay.Contracts.Validation;

namespace CloneEbay.Contracts.Stores;

public record CreateStoreRequest(
    [param: RequiredTrimmed]
    [param: StringLength(100, MinimumLength = 3, ErrorMessage = ValidationMessages.StringLength)]
    string storeName,

    [param: StringLength(1000, ErrorMessage = ValidationMessages.MaxLength)]
    string? description,

    [param: StringLength(500, ErrorMessage = ValidationMessages.MaxLength)]
    [param: Url(ErrorMessage = ValidationMessages.InvalidUrl)]
    string? bannerImageURL
);

public record UpdateStoreRequest(
    [param: RequiredTrimmed]
    [param: StringLength(100, MinimumLength = 3, ErrorMessage = ValidationMessages.StringLength)]
    string storeName,

    [param: StringLength(1000, ErrorMessage = ValidationMessages.MaxLength)]
    string? description,

    [param: StringLength(500, ErrorMessage = ValidationMessages.MaxLength)]
    [param: Url(ErrorMessage = ValidationMessages.InvalidUrl)]
    string? bannerImageURL
);

public record StoreDto(
    int id,
    int sellerId,
    string storeName,
    string? description,
    string? bannerImageURL
);