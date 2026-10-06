using AuthService.Application.Abstractions;
using AuthService.Application.Models;
using AuthService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthApplicationService authService) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, Metadata(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken cancellationToken)
        => authService.LoginAsync(request, Metadata(), cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    public Task<TokenResponse> Refresh(RefreshRequest request, CancellationToken cancellationToken)
        => authService.RefreshAsync(request, Metadata(), cancellationToken);

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, Metadata(), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Policy = Permissions.ProfileRead)]
    public Task<CurrentUserResponse> Me(CancellationToken cancellationToken)
        => authService.GetCurrentUserAsync(CurrentUserId(), cancellationToken);
}
