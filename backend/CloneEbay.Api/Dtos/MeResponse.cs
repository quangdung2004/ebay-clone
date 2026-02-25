namespace CloneEbay.Api.Dtos;

public record MeResponse(
    string Username,
    string Email,
    string Role,
    string? AvatarURL
);
