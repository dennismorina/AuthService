using AuthService.Application.Models;

namespace AuthService.Application.Abstractions;

public interface IAuthApplicationService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request, RequestMetadata metadata, CancellationToken cancellationToken);
    Task<TokenResponse> LoginAsync(LoginRequest request, RequestMetadata metadata, CancellationToken cancellationToken);
    Task<TokenResponse> RefreshAsync(RefreshRequest request, RequestMetadata metadata, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, RequestMetadata metadata, CancellationToken cancellationToken);
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IAdministrationService
{
    Task<IReadOnlyList<UserAdminResponse>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task ReplaceUserRolesAsync(Guid actorUserId, Guid userId, IReadOnlyList<string> roles, RequestMetadata metadata, CancellationToken cancellationToken);
    Task ReplaceUserPermissionsAsync(Guid actorUserId, Guid userId, IReadOnlyList<string> permissions, RequestMetadata metadata, CancellationToken cancellationToken);
    Task ReplaceRolePermissionsAsync(Guid actorUserId, string roleName, IReadOnlyList<string> permissions, RequestMetadata metadata, CancellationToken cancellationToken);
    Task<IReadOnlyList<SecurityEventResponse>> GetSecurityEventsAsync(int limit, CancellationToken cancellationToken);
}
