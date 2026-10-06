using AuthService.Application.Exceptions;
using AuthService.Application.Security;
using AuthService.Domain.Entities;
using Xunit;

namespace AuthService.UnitTests;

public sealed class UserSecurityTests
{
    [Fact]
    public void FailedLogins_LockAccountAtConfiguredThreshold()
    {
        var now = new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
        var user = User.Create("user@example.com", "USER@EXAMPLE.COM", now);

        Assert.False(user.RecordFailedLogin(now, 3, TimeSpan.FromMinutes(15)));
        Assert.False(user.RecordFailedLogin(now, 3, TimeSpan.FromMinutes(15)));
        Assert.True(user.RecordFailedLogin(now, 3, TimeSpan.FromMinutes(15)));
        Assert.True(user.IsLocked(now.AddMinutes(1)));
        Assert.False(user.IsLocked(now.AddMinutes(16)));
    }

    [Fact]
    public void SuccessfulLogin_ResetsLockoutState()
    {
        var now = new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
        var user = User.Create("user@example.com", "USER@EXAMPLE.COM", now);
        _ = user.RecordFailedLogin(now, 1, TimeSpan.FromMinutes(15));

        user.RecordSuccessfulLogin(now.AddMinutes(1));

        Assert.False(user.IsLocked(now.AddMinutes(1)));
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Equal(now.AddMinutes(1), user.LastLoginAtUtc);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase123!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NoDigitsHere!!!!")]
    [InlineData("NoSpecialChar123")]
    public void PasswordPolicy_RejectsWeakPasswords(string password)
    {
        Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password, 12));
    }

    [Fact]
    public void PasswordPolicy_AcceptsStrongPassword()
    {
        PasswordPolicy.Validate("StrongPassword2026!", 12);
    }
}
