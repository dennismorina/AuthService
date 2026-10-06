namespace AuthService.Application.Models;

public sealed record UserAdminResponse(
    Guid Id,
    string Email,
    bool IsActive,
    int FailedLoginAttempts,
    DateTimeOffset? LockoutEndUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> DirectPermissions,
    IReadOnlyList<string> EffectivePermissions);

public sealed record RoleResponse(string Name, IReadOnlyList<string> Permissions);

public sealed record SecurityEventResponse(
    Guid Id,
    Guid? UserId,
    string Type,
    bool Succeeded,
    string? IpAddress,
    string? UserAgent,
    string? Details,
    DateTimeOffset OccurredAtUtc);

public sealed record ReplaceRolesRequest(IReadOnlyList<string> Roles);
public sealed record ReplacePermissionsRequest(IReadOnlyList<string> Permissions);
