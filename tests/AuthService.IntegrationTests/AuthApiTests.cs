using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthService.Application.Models;
using Xunit;

namespace AuthService.IntegrationTests;

public sealed class AuthApiTests : IClassFixture<AuthServiceApiFactory>
{
    private readonly AuthServiceApiFactory _factory;

    public AuthApiTests(AuthServiceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ThenMe_ReturnsAuthenticatedUser()
    {
        using var client = _factory.CreateClient();
        var tokens = await RegisterAsync(client, "user1@test.local", "StrongPassword2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestContext.Current.CancellationToken);

        Assert.NotNull(me);
        Assert.Equal("user1@test.local", me.Email);
        Assert.Contains("User", me.Roles);
        Assert.Contains("profile.read", me.Permissions);
    }

    [Fact]
    public async Task NormalUser_CannotReadAdminUsers()
    {
        using var client = _factory.CreateClient();
        var tokens = await RegisterAsync(client, "user2@test.local", "StrongPassword2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await client.GetAsync("/api/admin/users", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BootstrapAdmin_CanReadAdminUsers()
    {
        using var client = _factory.CreateClient();
        var tokens = await LoginAsync(client, "admin@test.local", "AdminPassword2026!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await client.GetAsync("/api/admin/users", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_Rotates_AndReuseRevokesFamily()
    {
        using var client = _factory.CreateClient();
        var login = await LoginAsync(client, "admin@test.local", "AdminPassword2026!");

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken), TestContext.Current.CancellationToken);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotNull(rotated);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        var familyResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rotated.RefreshToken), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, familyResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidPassword_ReturnsGenericUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@test.local", "DefinitelyWrong2026!"), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Invalid email or password", body, StringComparison.Ordinal);
    }

    private static async Task<TokenResponse> RegisterAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private static async Task<TokenResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }
}
