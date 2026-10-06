using AuthService.Domain.Entities;

namespace AuthService.Application.Abstractions;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken);
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken);
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken cancellationToken);
    Task<Permission?> GetPermissionByNameAsync(string permissionName, CancellationToken cancellationToken);
    Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetDirectPermissionNamesAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRolePermissionNamesAsync(Guid roleId, CancellationToken cancellationToken);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyList<RefreshToken>> GetRefreshTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SecurityEvent>> GetSecurityEventsAsync(int limit, CancellationToken cancellationToken);
    Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);
    Task ReplaceUserPermissionsAsync(Guid userId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken);
    Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken);
    void AddUser(User user);
    void AddRole(Role role);
    void AddPermission(Permission permission);
    void AddUserRole(UserRole userRole);
    void AddRolePermission(RolePermission rolePermission);
    void AddRefreshToken(RefreshToken refreshToken);
    void AddSecurityEvent(SecurityEvent securityEvent);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void ClearTracking();
}
