using System.ComponentModel.DataAnnotations;
using CloneEbay.Contracts.Validation;

namespace CloneEbay.Contracts.Auth;

public record RegisterRequest(
    [param: RequiredTrimmed]
    [param: StringLength(50, MinimumLength = 3, ErrorMessage = ValidationMessages.StringLength)]
    string Username,

    [param: RequiredTrimmed]
    [param: EmailAddress(ErrorMessage = ValidationMessages.InvalidEmail)]
    [param: StringLength(255, ErrorMessage = ValidationMessages.MaxLength)]
    string Email,

    [param: RequiredTrimmed]
    [param: PasswordRule(6)]
    string Password
);

public record LoginRequest(
    [param: RequiredTrimmed]
    [param: EmailAddress(ErrorMessage = ValidationMessages.InvalidEmail)]
    string Email,

    [param: RequiredTrimmed]
    string Password,

    bool RememberMe = false
);

public record AuthResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    string AccessToken,
    int ExpiresInMinutes
);

public record RefreshResponse(string AccessToken, int ExpiresInMinutes);

public record VerifyEmailRequest(
    [param: RequiredTrimmed]
    string Token
);

public record ForgotPasswordRequest(
    [param: RequiredTrimmed]
    [param: EmailAddress(ErrorMessage = ValidationMessages.InvalidEmail)]
    string Email
);

public record ResetPasswordRequest(
    [param: RequiredTrimmed]
    string Token,

    [param: RequiredTrimmed]
    [param: PasswordRule(6)]
    string NewPassword
);

public record ResendVerifyEmailRequest(
    [param: RequiredTrimmed]
    [param: EmailAddress(ErrorMessage = ValidationMessages.InvalidEmail)]
    string Email
);