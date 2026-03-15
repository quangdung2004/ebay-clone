using System.ComponentModel.DataAnnotations;
using CloneEbay.Contracts.Validation;

namespace CloneEbay.Contracts.Addresses;

public record AddressDto(
    int id,
    string? fullName,
    string? phone,
    string? street,
    string? city,
    string? state,
    string? country,
    bool isDefault
);

public record CreateAddressRequest(
    [param: RequiredTrimmed]
    [param: StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    string fullName,

    [param: RequiredTrimmed]
    [param: StringLength(20, ErrorMessage = ValidationMessages.MaxLength)]
    string phone,

    [param: RequiredTrimmed]
    [param: StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    string street,

    [param: RequiredTrimmed]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string city,

    [param: RequiredTrimmed]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string state,

    [param: RequiredTrimmed]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string country,

    bool isDefault
);

public record UpdateAddressRequest(
    [param: RequiredTrimmed]
    [param: StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    string fullName,

    [param: RequiredTrimmed]
    [param: StringLength(20, ErrorMessage = ValidationMessages.MaxLength)]
    string phone,

    [param: RequiredTrimmed]
    [param: StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    string street,

    [param: RequiredTrimmed]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string city,

    [param: RequiredTrimmed]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string state,

    [param: RequiredTrimmed]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string country,

    bool isDefault
);
