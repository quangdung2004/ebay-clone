using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Contracts.Validation;

public sealed class PasswordRuleAttribute : ValidationAttribute
{
    private readonly int _minLength;

    public PasswordRuleAttribute(int minLength = 6)
    {
        _minLength = minLength;
        ErrorMessage = ValidationMessages.MinLength;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        if (value is not string s)
            return new ValidationResult("Invalid password format.");

        if (string.IsNullOrWhiteSpace(s))
        {
            return new ValidationResult(
                string.Format(ValidationMessages.Required, validationContext.DisplayName));
        }

        if (s.Length < _minLength)
        {
            return new ValidationResult(
                string.Format(ValidationMessages.MinLength, validationContext.DisplayName, _minLength));
        }

        return ValidationResult.Success;
    }
}