namespace CloneEbay.Contracts.Auth;

public record MeResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    string? AvatarURL
);
