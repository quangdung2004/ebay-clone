namespace CloneEbay.Api.Dtos;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);

public record AuthResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    string AccessToken
);
