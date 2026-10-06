namespace AuthService.Application.Configuration;

public sealed class SecurityOptions
{
    public int AccessTokenMinutes { get; set; } = 10;
    public int RefreshTokenDays { get; set; } = 14;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int MinimumPasswordLength { get; set; } = 12;
}
