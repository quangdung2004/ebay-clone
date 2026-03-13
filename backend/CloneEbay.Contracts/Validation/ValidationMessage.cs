namespace CloneEbay.Contracts.Validation;

public static class ValidationMessages
{
    public const string Required = "{0} is required.";
    public const string InvalidEmail = "{0} is not a valid email.";
    public const string StringLength = "{0} must be between {2} and {1} characters.";
    public const string MaxLength = "{0} must not exceed {1} characters.";
    public const string MinLength = "{0} must be at least {1} characters.";
    public const string PositiveNumber = "{0} must be greater than 0.";
    public const string NonNegativeNumber = "{0} must be greater than or equal to 0.";
    public const string InvalidUrl = "{0} must be a valid URL.";
}