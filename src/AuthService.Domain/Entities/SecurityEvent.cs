namespace AuthService.Domain.Entities;

public sealed class SecurityEvent
{
    private SecurityEvent()
    {
    }

    private SecurityEvent(
        Guid id,
        Guid? userId,
        string type,
        bool succeeded,
        string? ipAddress,
        string? userAgent,
        string? details,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Succeeded = succeeded;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Details = details;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public bool Succeeded { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static SecurityEvent Create(
        Guid? userId,
        string type,
        bool succeeded,
        string? ipAddress,
        string? userAgent,
        string? details,
        DateTimeOffset occurredAtUtc)
        => new(Guid.NewGuid(), userId, type, succeeded, ipAddress, userAgent, details, occurredAtUtc);
}
