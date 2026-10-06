namespace AuthService.Application.Models;

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);

public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);
