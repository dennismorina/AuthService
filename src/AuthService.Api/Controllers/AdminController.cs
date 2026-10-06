using AuthService.Application.Abstractions;
using AuthService.Application.Models;
using AuthService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminController(IAdministrationService administrationService) : ApiControllerBase
{
    [HttpGet("ping")]
    [Authorize(Policy = Permissions.AdminAccess)]
    public IActionResult Ping() => Ok(new { status = "authorized" });

    [HttpGet("users")]
    [Authorize(Policy = Permissions.UsersRead)]
    public Task<IReadOnlyList<UserAdminResponse>> GetUsers(CancellationToken cancellationToken)
        => administrationService.GetUsersAsync(cancellationToken);

    [HttpPut("users/{userId:guid}/roles")]
    [Authorize(Policy = Permissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReplaceUserRoles(
        Guid userId,
        ReplaceRolesRequest request,
        CancellationToken cancellationToken)
    {
        await administrationService.ReplaceUserRolesAsync(CurrentUserId(), userId, request.Roles, Metadata(), cancellationToken);
        return NoContent();
    }

    [HttpPut("users/{userId:guid}/permissions")]
    [Authorize(Policy = Permissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReplaceUserPermissions(
        Guid userId,
        ReplacePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        await administrationService.ReplaceUserPermissionsAsync(CurrentUserId(), userId, request.Permissions, Metadata(), cancellationToken);
        return NoContent();
    }

    [HttpGet("roles")]
    [Authorize(Policy = Permissions.RolesRead)]
    public Task<IReadOnlyList<RoleResponse>> GetRoles(CancellationToken cancellationToken)
        => administrationService.GetRolesAsync(cancellationToken);

    [HttpPut("roles/{roleName}/permissions")]
    [Authorize(Policy = Permissions.RolesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReplaceRolePermissions(
        string roleName,
        ReplacePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        await administrationService.ReplaceRolePermissionsAsync(CurrentUserId(), roleName, request.Permissions, Metadata(), cancellationToken);
        return NoContent();
    }

    [HttpGet("permissions")]
    [Authorize(Policy = Permissions.RolesRead)]
    public Task<IReadOnlyList<string>> GetPermissions(CancellationToken cancellationToken)
        => administrationService.GetPermissionsAsync(cancellationToken);

    [HttpGet("security-events")]
    [Authorize(Policy = Permissions.SecurityEventsRead)]
    public Task<IReadOnlyList<SecurityEventResponse>> GetSecurityEvents([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
        => administrationService.GetSecurityEventsAsync(limit, cancellationToken);
}
