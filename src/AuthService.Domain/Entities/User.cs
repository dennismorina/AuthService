namespace AuthService.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    private User(Guid id, string email, string normalizedEmail, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public static User Create(string email, string normalizedEmail, DateTimeOffset createdAtUtc)
        => new(Guid.NewGuid(), email, normalizedEmail, createdAtUtc);

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public bool IsLocked(DateTimeOffset nowUtc)
        => LockoutEndUtc is not null && LockoutEndUtc > nowUtc;

    public bool RecordFailedLogin(DateTimeOffset nowUtc, int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts < maxFailedAttempts)
            return false;

        FailedLoginAttempts = 0;
        LockoutEndUtc = nowUtc.Add(lockoutDuration);
        return true;
    }

    public void RecordSuccessfulLogin(DateTimeOffset nowUtc)
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = nowUtc;
    }
}
