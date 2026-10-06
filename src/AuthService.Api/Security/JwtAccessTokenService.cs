using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Application.Abstractions;
using AuthService.Application.Configuration;
using AuthService.Application.Models;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Api.Security;

public sealed class JwtAccessTokenService(
    IOptions<JwtOptions> jwtOptions,
    IOptions<SecurityOptions> securityOptions,
    TimeProvider timeProvider) : IAccessTokenService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly SecurityOptions _security = securityOptions.Value;

    public AccessTokenResult Create(User user, IReadOnlyList<string> roles)
    {
        if (Encoding.UTF8.GetByteCount(_jwt.SigningKey) < 32)
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes long.");

        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_security.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
