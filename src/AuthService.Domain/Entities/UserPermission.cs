namespace AuthService.Domain.Entities;

public sealed class UserPermission
{
    private UserPermission()
    {
    }

    public UserPermission(Guid userId, Guid permissionId)
    {
        UserId = userId;
        PermissionId = permissionId;
    }

    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }
}
