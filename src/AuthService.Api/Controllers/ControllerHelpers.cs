using System.IdentityModel.Tokens.Jwt;
using AuthService.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId()
    {
        var value = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Missing user id claim.");
    }

    protected RequestMetadata Metadata()
        => new(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString());
}
