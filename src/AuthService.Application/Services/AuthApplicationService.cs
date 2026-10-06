using System.ComponentModel.DataAnnotations;
using AuthService.Application.Abstractions;
using AuthService.Application.Configuration;
using AuthService.Application.Exceptions;
using AuthService.Application.Models;
using AuthService.Application.Security;
using AuthService.Domain.Authorization;
using AuthService.Domain.Entities;
using AuthService.Domain.Security;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services;

public sealed class AuthApplicationService(
    IAuthRepository repository,
    IPasswordService passwordService,
    IAccessTokenService accessTokenService,
    IOptions<SecurityOptions> securityOptions,
    TimeProvider timeProvider) : IAuthApplicationService
{
    private readonly SecurityOptions _security = securityOptions.Value;

    public async Task<TokenResponse> RegisterAsync(
        RegisterRequest request,
        RequestMetadata metadata,
        CancellationToken cancellationToken)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        PasswordPolicy.Validate(request.Password, _security.MinimumPasswordLength);

        if (await repository.GetUserByEmailAsync(email.Normalized, cancellationToken) is not null)
            throw new ConflictException("A user with this email already exists.");

        var now = timeProvider.GetUtcNow();
        var user = User.Create(email.Original, email.Normalized, now);
        user.SetPasswordHash(passwordService.Hash(user, request.Password));

        var userRole = await repository.GetRoleByNameAsync(BuiltInRoles.User, cancellationToken)
            ?? throw new InvalidOperationException("Built-in User role is missing.");

        var refresh = CreateRefreshToken(user.Id, Guid.NewGuid(), now);

        repository.AddUser(user);
        repository.AddUserRole(new UserRole(user.Id, userRole.Id));
        repository.AddRefreshToken(refresh.Entity);
        repository.AddSecurityEvent(CreateEvent(user.Id, SecurityEventTypes.RegistrationSucceeded, true, metadata, null, now));

        await repository.SaveChangesAsync(cancellationToken);

        var access = accessTokenService.Create(user, [BuiltInRoles.User]);
        return new TokenResponse(access.Token, access.ExpiresAtUtc, refresh.RawToken, refresh.Entity.ExpiresAtUtc);
    }

    public async Task<TokenResponse> LoginAsync(
        LoginRequest request,
        RequestMetadata metadata,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(request.Email);
        var now = timeProvider.GetUtcNow();
        var user = await repository.GetUserByEmailAsync(normalized, cancellationToken);

        if (user is null)
        {
            passwordService.VerifyAgainstDummy(request.Password);
            repository.AddSecurityEvent(CreateEvent(null, SecurityEventTypes.LoginFailed, false, metadata, "invalid-credentials", now));
            await repository.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        if (!user.IsActive || user.IsLocked(now))
        {
            repository.AddSecurityEvent(CreateEvent(user.Id, SecurityEventTypes.LoginFailed, false, metadata, "account-unavailable", now));
            await repository.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        var passwordCheck = passwordService.Verify(user, user.PasswordHash, request.Password);
        if (passwordCheck == PasswordCheckResult.Failed)
        {
            var locked = user.RecordFailedLogin(
                now,
                _security.MaxFailedLoginAttempts,
                TimeSpan.FromMinutes(_security.LockoutMinutes));

            repository.AddSecurityEvent(CreateEvent(user.Id, SecurityEventTypes.LoginFailed, false, metadata, "invalid-credentials", now));
            if (locked)
                repository.AddSecurityEvent(CreateEvent(user.Id, SecurityEventTypes.AccountLocked, false, metadata, null, now));

            await repository.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        if (passwordCheck == PasswordCheckResult.SuccessRehashNeeded)
            user.SetPasswordHash(passwordService.Hash(user, request.Password));

        user.RecordSuccessfulLogin(now);
        var roles = await repository.GetRoleNamesAsync(user.Id, cancellationToken);
        var refresh = CreateRefreshToken(user.Id, Guid.NewGuid(), now);
        repository.AddRefreshToken(refresh.Entity);
        repository.AddSecurityEvent(CreateEvent(user.Id, SecurityEventTypes.LoginSucceeded, true, metadata, null, now));

        await repository.SaveChangesAsync(cancellationToken);

        var access = accessTokenService.Create(user, roles);
        return new TokenResponse(access.Token, access.ExpiresAtUtc, refresh.RawToken, refresh.Entity.ExpiresAtUtc);
    }

    public async Task<TokenResponse> RefreshAsync(
        RefreshRequest request,
        RequestMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw InvalidRefreshToken();

        var now = timeProvider.GetUtcNow();
        var hash = RefreshTokenCodec.Hash(request.RefreshToken);
        var current = await repository.GetRefreshTokenByHashAsync(hash, cancellationToken);

        if (current is null)
            throw InvalidRefreshToken();

        if (current.IsExpired(now))
            throw InvalidRefreshToken();

        if (current.UsedAtUtc is not null || current.RevokedAtUtc is not null)
        {
            await RevokeFamilyForReuseAsync(current.FamilyId, current.UserId, metadata, now, cancellationToken);
            throw InvalidRefreshToken();
        }

        var user = await repository.GetUserByIdAsync(current.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw InvalidRefreshToken();

        var roles = await repository.GetRoleNamesAsync(user.Id, cancellationToken);
        var replacement = CreateRefreshToken(user.Id, current.FamilyId, now);
        current.MarkRotated(replacement.Entity.Id, now);
        repository.AddRefreshToken(replacement.Entity);
        repository.AddSecurityEvent(CreateEvent(user.Id, SecurityEventTypes.RefreshSucceeded, true, metadata, null, now));

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            repository.ClearTracking();
            var reloaded = await repository.GetRefreshTokenByHashAsync(hash, cancellationToken);
            if (reloaded is not null)
                await RevokeFamilyForReuseAsync(reloaded.FamilyId, reloaded.UserId, metadata, now, cancellationToken);

            throw InvalidRefreshToken();
        }

        var access = accessTokenService.Create(user, roles);
        return new TokenResponse(access.Token, access.ExpiresAtUtc, replacement.RawToken, replacement.Entity.ExpiresAtUtc);
    }

    public async Task LogoutAsync(
        LogoutRequest request,
        RequestMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;

        var now = timeProvider.GetUtcNow();
        var hash = RefreshTokenCodec.Hash(request.RefreshToken);
        var current = await repository.GetRefreshTokenByHashAsync(hash, cancellationToken);
        if (current is null)
            return;

        var family = await repository.GetRefreshTokenFamilyAsync(current.FamilyId, cancellationToken);
        foreach (var token in family)
            token.Revoke(now, "logout");

        repository.AddSecurityEvent(CreateEvent(current.UserId, SecurityEventTypes.Logout, true, metadata, null, now));
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var roles = await repository.GetRoleNamesAsync(userId, cancellationToken);
        var permissions = await repository.GetEffectivePermissionsAsync(userId, cancellationToken);

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            roles,
            permissions.OrderBy(static x => x, StringComparer.Ordinal).ToArray());
    }

    private async Task RevokeFamilyForReuseAsync(
        Guid familyId,
        Guid userId,
        RequestMetadata metadata,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var family = await repository.GetRefreshTokenFamilyAsync(familyId, cancellationToken);
        foreach (var token in family)
            token.Revoke(now, "refresh-token-reuse-detected");

        repository.AddSecurityEvent(CreateEvent(userId, SecurityEventTypes.RefreshReuseDetected, false, metadata, null, now));
        await repository.SaveChangesAsync(cancellationToken);
    }

    private (string Original, string Normalized) NormalizeAndValidateEmail(string email)
    {
        var original = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(original) || !new EmailAddressAttribute().IsValid(original))
            throw new Exceptions.ValidationException("A valid email address is required.");

        return (original, NormalizeEmail(original));
    }

    private static string NormalizeEmail(string email)
        => (email ?? string.Empty).Trim().ToUpperInvariant();

    private (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId, Guid familyId, DateTimeOffset now)
    {
        var raw = RefreshTokenCodec.Generate();
        var entity = RefreshToken.Create(
            userId,
            familyId,
            RefreshTokenCodec.Hash(raw),
            now,
            now.AddDays(_security.RefreshTokenDays));

        return (raw, entity);
    }

    private static SecurityEvent CreateEvent(
        Guid? userId,
        string type,
        bool succeeded,
        RequestMetadata metadata,
        string? details,
        DateTimeOffset now)
        => SecurityEvent.Create(userId, type, succeeded, metadata.IpAddress, metadata.UserAgent, details, now);

    private static AuthenticationFailedException InvalidCredentials()
        => new("Invalid email or password.");

    private static AuthenticationFailedException InvalidRefreshToken()
        => new("Invalid refresh token.");
}
