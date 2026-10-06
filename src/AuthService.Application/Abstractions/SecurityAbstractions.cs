using AuthService.Application.Models;
using AuthService.Domain.Entities;

namespace AuthService.Application.Abstractions;

public enum PasswordCheckResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}

public interface IPasswordService
{
    string Hash(User user, string password);
    PasswordCheckResult Verify(User user, string passwordHash, string password);
    void VerifyAgainstDummy(string password);
}

public interface IAccessTokenService
{
    AccessTokenResult Create(User user, IReadOnlyList<string> roles);
}

public interface IPermissionEvaluator
{
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}
