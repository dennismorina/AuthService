using AuthService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.IntegrationTests;

public sealed class AuthServiceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"authservice-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=unused;Database=unused;Username=unused;Password=unused",
                ["Database:Initialize"] = "true",
                ["Jwt:Issuer"] = "AuthService.Tests",
                ["Jwt:Audience"] = "AuthService.Tests.Client",
                ["Jwt:SigningKey"] = "AuthService-Integration-Test-Signing-Key-2026-ABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890",
                ["BootstrapAdmin:Enabled"] = "true",
                ["BootstrapAdmin:Email"] = "admin@test.local",
                ["BootstrapAdmin:Password"] = "AdminPassword2026!"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(DbContextOptions<AuthDbContext>) ||
                                     descriptor.ServiceType == typeof(IDbContextOptionsConfiguration<AuthDbContext>))
                .ToArray();

            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            services.AddDbContext<AuthDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
