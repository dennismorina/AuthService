namespace AuthService.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? RevocationReason { get; private set; }
    public long Version { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        => new(Guid.NewGuid(), userId, familyId, tokenHash, createdAtUtc, expiresAtUtc);

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresAtUtc <= nowUtc;

    public bool IsReusable(DateTimeOffset nowUtc)
        => !IsExpired(nowUtc) && UsedAtUtc is null && RevokedAtUtc is null;

    public void MarkRotated(Guid replacementTokenId, DateTimeOffset nowUtc)
    {
        UsedAtUtc = nowUtc;
        ReplacedByTokenId = replacementTokenId;
        Version++;
    }

    public void Revoke(DateTimeOffset nowUtc, string reason)
    {
        if (RevokedAtUtc is not null)
            return;

        RevokedAtUtc = nowUtc;
        RevocationReason = reason;
        Version++;
    }
}
