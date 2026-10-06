namespace AuthService.Application.Configuration;

public sealed class BootstrapAdminOptions
{
    public bool Enabled { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
