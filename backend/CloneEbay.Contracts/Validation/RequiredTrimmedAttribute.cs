using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Contracts.Validation;

public sealed class RequiredTrimmedAttribute : ValidationAttribute
{
    public RequiredTrimmedAttribute()
    {
        ErrorMessage = ValidationMessages.Required;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

        if (value is string s && string.IsNullOrWhiteSpace(s))
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

        return ValidationResult.Success;
    }
}