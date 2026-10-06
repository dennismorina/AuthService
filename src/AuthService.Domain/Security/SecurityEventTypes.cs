namespace AuthService.Domain.Security;

public static class SecurityEventTypes
{
    public const string RegistrationSucceeded = "registration.succeeded";
    public const string LoginSucceeded = "login.succeeded";
    public const string LoginFailed = "login.failed";
    public const string AccountLocked = "account.locked";
    public const string RefreshSucceeded = "refresh.succeeded";
    public const string RefreshReuseDetected = "refresh.reuse-detected";
    public const string Logout = "logout";
    public const string UserRolesChanged = "user.roles-changed";
    public const string UserPermissionsChanged = "user.permissions-changed";
    public const string RolePermissionsChanged = "role.permissions-changed";
}
