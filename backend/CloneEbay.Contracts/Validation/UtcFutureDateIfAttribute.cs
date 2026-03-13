using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Contracts.Validation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class UtcFutureDateIfAttribute : ValidationAttribute
{
    private readonly string _boolPropertyName;
    private readonly string _datePropertyName;

    public UtcFutureDateIfAttribute(string boolPropertyName, string datePropertyName)
    {
        _boolPropertyName = boolPropertyName;
        _datePropertyName = datePropertyName;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        var type = value.GetType();
        var boolProp = type.GetProperty(_boolPropertyName);
        var dateProp = type.GetProperty(_datePropertyName);

        if (boolProp is null || dateProp is null)
            return ValidationResult.Success;

        var boolValue = boolProp.GetValue(value);
        var isEnabled = boolValue is bool b && b;

        if (!isEnabled)
            return ValidationResult.Success;

        var rawDate = dateProp.GetValue(value);

        if (rawDate is null)
        {
            return new ValidationResult(
                $"{_datePropertyName} is required when {_boolPropertyName} is true.");
        }

        if (rawDate is not DateTime dateValue)
        {
            return new ValidationResult($"{_datePropertyName} is invalid.");
        }

        if (dateValue <= DateTime.UtcNow)
        {
            return new ValidationResult($"{_datePropertyName} must be in the future.");
        }

        return ValidationResult.Success;
    }
}