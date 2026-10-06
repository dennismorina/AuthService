using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.Api.Security;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler(IPermissionEvaluator evaluator)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var sub = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var userId))
            return;

        if (await evaluator.HasPermissionAsync(userId, requirement.Permission, CancellationToken.None))
            context.Succeed(requirement);
    }
}
