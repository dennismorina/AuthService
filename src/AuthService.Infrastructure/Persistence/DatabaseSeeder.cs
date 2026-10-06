using AuthService.Application.Abstractions;
using AuthService.Application.Configuration;
using AuthService.Domain.Authorization;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    AuthDbContext dbContext,
    IPasswordService passwordService,
    IOptions<BootstrapAdminOptions> bootstrapOptions,
    TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissionMap = new Dictionary<string, Permission>(StringComparer.Ordinal);
        foreach (var permissionName in Permissions.All)
        {
            var permission = await dbContext.Permissions.SingleOrDefaultAsync(x => x.Name == permissionName, cancellationToken);
            if (permission is null)
            {
                permission = Permission.Create(permissionName);
                dbContext.Permissions.Add(permission);
            }

            permissionMap[permissionName] = permission;
        }

        var adminRole = await GetOrCreateRoleAsync(BuiltInRoles.Admin, cancellationToken);
        var userRole = await GetOrCreateRoleAsync(BuiltInRoles.User, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureRolePermissionsAsync(adminRole.Id, permissionMap.Values.Select(x => x.Id).ToArray(), cancellationToken);
        await EnsureRolePermissionsAsync(userRole.Id, [permissionMap[Permissions.ProfileRead].Id], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var bootstrap = bootstrapOptions.Value;
        if (!bootstrap.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(bootstrap.Email) || string.IsNullOrWhiteSpace(bootstrap.Password))
            throw new InvalidOperationException("Bootstrap admin is enabled but email or password is missing.");

        var normalizedEmail = bootstrap.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null)
        {
            user = User.Create(bootstrap.Email.Trim(), normalizedEmail, timeProvider.GetUtcNow());
            user.SetPasswordHash(passwordService.Hash(user, bootstrap.Password));
            dbContext.Users.Add(user);
            dbContext.UserRoles.Add(new UserRole(user.Id, adminRole.Id));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var hasAdmin = await dbContext.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == adminRole.Id, cancellationToken);
            if (!hasAdmin)
            {
                dbContext.UserRoles.Add(new UserRole(user.Id, adminRole.Id));
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task<Role> GetOrCreateRoleAsync(string name, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (role is not null)
            return role;

        role = Role.Create(name);
        dbContext.Roles.Add(role);
        return role;
    }

    private async Task EnsureRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var existing = await dbContext.RolePermissions
            .Where(x => x.RoleId == roleId)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in permissionIds.Except(existing))
            dbContext.RolePermissions.Add(new RolePermission(roleId, permissionId));
    }
}
