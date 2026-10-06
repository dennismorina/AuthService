namespace AuthService.Domain.Authorization;

public static class Permissions
{
    public const string ProfileRead = "profile.read";
    public const string AdminAccess = "admin.access";
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";
    public const string SecurityEventsRead = "security-events.read";

    public static IReadOnlyList<string> All { get; } =
    [
        ProfileRead,
        AdminAccess,
        UsersRead,
        UsersWrite,
        RolesRead,
        RolesWrite,
        SecurityEventsRead
    ];
}
