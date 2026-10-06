namespace AuthService.Application.Configuration;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "AuthService";
    public string Audience { get; set; } = "AuthService.Client";
    public string SigningKey { get; set; } = string.Empty;
}
