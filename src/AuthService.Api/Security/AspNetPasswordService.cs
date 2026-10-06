using AuthService.Application.Abstractions;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Api.Security;

public sealed class AspNetPasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();
    private readonly User _dummyUser;
    private readonly string _dummyHash;

    public AspNetPasswordService()
    {
        _dummyUser = User.Create("dummy@example.invalid", "DUMMY@EXAMPLE.INVALID", DateTimeOffset.UnixEpoch);
        _dummyHash = _hasher.HashPassword(_dummyUser, "Dummy-Password-For-Timing-Only-2026!");
    }

    public string Hash(User user, string password)
        => _hasher.HashPassword(user, password);

    public PasswordCheckResult Verify(User user, string passwordHash, string password)
        => _hasher.VerifyHashedPassword(user, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };

    public void VerifyAgainstDummy(string password)
        => _ = _hasher.VerifyHashedPassword(_dummyUser, _dummyHash, password);
}
