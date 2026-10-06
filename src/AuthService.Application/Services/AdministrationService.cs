using AuthService.Application.Abstractions;
using AuthService.Application.Exceptions;
using AuthService.Application.Models;
using AuthService.Domain.Entities;
using AuthService.Domain.Security;

namespace AuthService.Application.Services;

public sealed class AdministrationService(
    IAuthRepository repository,
    TimeProvider timeProvider) : IAdministrationService
{
    public async Task<IReadOnlyList<UserAdminResponse>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await repository.GetUsersAsync(cancellationToken);
        var result = new List<UserAdminResponse>(users.Count);

        foreach (var user in users)
        {
            var roles = await repository.GetRoleNamesAsync(user.Id, cancellationToken);
            var direct = await repository.GetDirectPermissionNamesAsync(user.Id, cancellationToken);
            var effective = await repository.GetEffectivePermissionsAsync(user.Id, cancellationToken);

            result.Add(new UserAdminResponse(
                user.Id,
                user.Email,
                user.IsActive,
                user.FailedLoginAttempts,
                user.LockoutEndUtc,
                user.CreatedAtUtc,
                user.LastLoginAtUtc,
                roles,
                direct,
                effective.OrderBy(static x => x, StringComparer.Ordinal).ToArray()));
        }

        return result;
    }

    public async Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await repository.GetRolesAsync(cancellationToken);
        var result = new List<RoleResponse>(roles.Count);

        foreach (var role in roles)
        {
            var permissions = await repository.GetRolePermissionNamesAsync(role.Id, cancellationToken);
            result.Add(new RoleResponse(role.Name, permissions));
        }

        return result;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await repository.GetPermissionsAsync(cancellationToken);
        return permissions.Select(static x => x.Name).OrderBy(static x => x, StringComparer.Ordinal).ToArray();
    }

    public async Task ReplaceUserRolesAsync(
        Guid actorUserId,
        Guid userId,
        IReadOnlyList<string> roles,
        RequestMetadata metadata,
        CancellationToken cancellationToken)
    {
        _ = await repository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var roleIds = await ResolveRoleIdsAsync(roles, cancellationToken);
        await repository.ReplaceUserRolesAsync(userId, roleIds, cancellationToken);
        repository.AddSecurityEvent(CreateAdminEvent(actorUserId, SecurityEventTypes.UserRolesChanged, metadata, $"targetUserId={userId}"));
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceUserPermissionsAsync(
        Guid actorUserId,
        Guid userId,
        IReadOnlyList<string> permissions,
        RequestMetadata metadata,
        CancellationToken cancellationToken)
    {
        _ = await repository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var permissionIds = await ResolvePermissionIdsAsync(permissions, cancellationToken);
        await repository.ReplaceUserPermissionsAsync(userId, permissionIds, cancellationToken);
        repository.AddSecurityEvent(CreateAdminEvent(actorUserId, SecurityEventTypes.UserPermissionsChanged, metadata, $"targetUserId={userId}"));
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceRolePermissionsAsync(
        Guid actorUserId,
        string roleName,
        IReadOnlyList<string> permissions,
        RequestMetadata metadata,
        CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleByNameAsync(roleName, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        var permissionIds = await ResolvePermissionIdsAsync(permissions, cancellationToken);
        await repository.ReplaceRolePermissionsAsync(role.Id, permissionIds, cancellationToken);
        repository.AddSecurityEvent(CreateAdminEvent(actorUserId, SecurityEventTypes.RolePermissionsChanged, metadata, $"role={role.Name}"));
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityEventResponse>> GetSecurityEventsAsync(int limit, CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var events = await repository.GetSecurityEventsAsync(safeLimit, cancellationToken);
        return events.Select(static x => new SecurityEventResponse(
            x.Id,
            x.UserId,
            x.Type,
            x.Succeeded,
            x.IpAddress,
            x.UserAgent,
            x.Details,
            x.OccurredAtUtc)).ToArray();
    }

    private async Task<Guid[]> ResolveRoleIdsAsync(IEnumerable<string> names, CancellationToken cancellationToken)
    {
        var distinct = names.Where(static x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var ids = new List<Guid>(distinct.Length);

        foreach (var name in distinct)
        {
            var role = await repository.GetRoleByNameAsync(name, cancellationToken)
                ?? throw new ValidationException($"Unknown role '{name}'.");
            ids.Add(role.Id);
        }

        return ids.ToArray();
    }

    private async Task<Guid[]> ResolvePermissionIdsAsync(IEnumerable<string> names, CancellationToken cancellationToken)
    {
        var distinct = names.Where(static x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        var ids = new List<Guid>(distinct.Length);

        foreach (var name in distinct)
        {
            var permission = await repository.GetPermissionByNameAsync(name, cancellationToken)
                ?? throw new ValidationException($"Unknown permission '{name}'.");
            ids.Add(permission.Id);
        }

        return ids.ToArray();
    }

    private SecurityEvent CreateAdminEvent(Guid actorUserId, string type, RequestMetadata metadata, string details)
        => SecurityEvent.Create(actorUserId, type, true, metadata.IpAddress, metadata.UserAgent, details, timeProvider.GetUtcNow());
}
