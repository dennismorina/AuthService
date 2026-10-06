using AuthService.Application.Abstractions;

namespace AuthService.Application.Services;

public sealed class PermissionEvaluator(IAuthRepository repository) : IPermissionEvaluator
{
    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        var permissions = await repository.GetEffectivePermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }
}
