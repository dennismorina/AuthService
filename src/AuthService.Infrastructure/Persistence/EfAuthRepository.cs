using AuthService.Application.Abstractions;
using AuthService.Application.Exceptions;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AuthService.Infrastructure.Persistence;

public sealed class EfAuthRepository(AuthDbContext dbContext) : IAuthRepository
{
    public Task<User?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken)
        => await dbContext.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync(cancellationToken);

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken)
        => dbContext.Roles.SingleOrDefaultAsync(x => x.Name.ToLower() == roleName.ToLower(), cancellationToken);

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken cancellationToken)
        => await dbContext.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<Permission?> GetPermissionByNameAsync(string permissionName, CancellationToken cancellationToken)
        => dbContext.Permissions.SingleOrDefaultAsync(x => x.Name == permissionName, cancellationToken);

    public async Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken cancellationToken)
        => await dbContext.Permissions.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken)
        => await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            orderby role.Name
            select role.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDirectPermissionNamesAsync(Guid userId, CancellationToken cancellationToken)
        => await (
            from userPermission in dbContext.UserPermissions
            join permission in dbContext.Permissions on userPermission.PermissionId equals permission.Id
            where userPermission.UserId == userId
            orderby permission.Name
            select permission.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rolePermissions =
            from userRole in dbContext.UserRoles
            join rolePermission in dbContext.RolePermissions on userRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId
            select permission.Name;

        var directPermissions =
            from userPermission in dbContext.UserPermissions
            join permission in dbContext.Permissions on userPermission.PermissionId equals permission.Id
            where userPermission.UserId == userId
            select permission.Name;

        var permissions = await rolePermissions.Union(directPermissions).AsNoTracking().ToListAsync(cancellationToken);
        return permissions.ToHashSet(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionNamesAsync(Guid roleId, CancellationToken cancellationToken)
        => await (
            from rolePermission in dbContext.RolePermissions
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where rolePermission.RoleId == roleId
            orderby permission.Name
            select permission.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetRefreshTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken)
        => await dbContext.RefreshTokens.Where(x => x.FamilyId == familyId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SecurityEvent>> GetSecurityEventsAsync(int limit, CancellationToken cancellationToken)
        => await dbContext.SecurityEvents
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserRoles.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        dbContext.UserRoles.RemoveRange(existing);
        foreach (var roleId in roleIds.Distinct())
            dbContext.UserRoles.Add(new UserRole(userId, roleId));
    }

    public async Task ReplaceUserPermissionsAsync(Guid userId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserPermissions.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        dbContext.UserPermissions.RemoveRange(existing);
        foreach (var permissionId in permissionIds.Distinct())
            dbContext.UserPermissions.Add(new UserPermission(userId, permissionId));
    }

    public async Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var existing = await dbContext.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(existing);
        foreach (var permissionId in permissionIds.Distinct())
            dbContext.RolePermissions.Add(new RolePermission(roleId, permissionId));
    }

    public void AddUser(User user) => dbContext.Users.Add(user);
    public void AddRole(Role role) => dbContext.Roles.Add(role);
    public void AddPermission(Permission permission) => dbContext.Permissions.Add(permission);
    public void AddUserRole(UserRole userRole) => dbContext.UserRoles.Add(userRole);
    public void AddRolePermission(RolePermission rolePermission) => dbContext.RolePermissions.Add(rolePermission);
    public void AddRefreshToken(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);
    public void AddSecurityEvent(SecurityEvent securityEvent) => dbContext.SecurityEvents.Add(securityEvent);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The security state changed concurrently.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException("A unique value already exists.");
        }
    }

    public void ClearTracking() => dbContext.ChangeTracker.Clear();
}
