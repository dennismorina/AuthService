using AuthService.Application.Security;
using AuthService.Domain.Entities;
using Xunit;

namespace AuthService.UnitTests;

public sealed class RefreshTokenTests
{
    [Fact]
    public void GeneratedRefreshTokens_AreRandomAndHashable()
    {
        var first = RefreshTokenCodec.Generate();
        var second = RefreshTokenCodec.Generate();

        Assert.NotEqual(first, second);
        Assert.Equal(64, RefreshTokenCodec.Hash(first).Length);
    }

    [Fact]
    public void RotatedToken_IsNoLongerReusable()
    {
        var now = new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), now, now.AddDays(14));

        token.MarkRotated(Guid.NewGuid(), now.AddMinutes(1));

        Assert.False(token.IsReusable(now.AddMinutes(2)));
        Assert.Equal(2, token.Version);
        Assert.NotNull(token.UsedAtUtc);
    }

    [Fact]
    public void RevokedToken_IsNoLongerReusable()
    {
        var now = new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), new string('B', 64), now, now.AddDays(14));

        token.Revoke(now.AddMinutes(1), "test");

        Assert.False(token.IsReusable(now.AddMinutes(2)));
        Assert.Equal("test", token.RevocationReason);
    }
}
