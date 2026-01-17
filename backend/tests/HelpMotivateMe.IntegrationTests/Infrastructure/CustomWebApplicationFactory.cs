using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelpMotivateMe.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add test configuration for OAuth (fake values to prevent validation errors)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OAuth:GitHub:ClientId"] = "test-client-id",
                ["OAuth:GitHub:ClientSecret"] = "test-client-secret",
                ["OAuth:Google:ClientId"] = "test-client-id",
                ["OAuth:Google:ClientSecret"] = "test-client-secret",
                ["OAuth:LinkedIn:ClientId"] = "test-client-id",
                ["OAuth:LinkedIn:ClientSecret"] = "test-client-secret"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test connection string
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // Override the default authentication scheme to use our test handler
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            });

            // Add our test authentication handler
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, _ => { });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create database and apply migrations
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
    }
}
