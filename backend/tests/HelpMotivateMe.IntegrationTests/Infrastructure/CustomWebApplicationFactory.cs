using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.Infrastructure.Services;
using HelpMotivateMe.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HelpMotivateMe.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly IReadOnlyDictionary<string, string?>? _configuration;
    private readonly bool _useTestAuthentication;

    public CustomWebApplicationFactory(
        string connectionString,
        bool useTestAuthentication = true,
        IReadOnlyDictionary<string, string?>? configuration = null)
    {
        _connectionString = connectionString;
        _useTestAuthentication = useTestAuthentication;
        _configuration = configuration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add test configuration for OAuth (fake values to prevent validation errors)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["SingleUser:Username"] = "test-admin",
                ["SingleUser:Password"] = "test-password-only",
                ["OAuth:GitHub:ClientId"] = "test-client-id",
                ["OAuth:GitHub:ClientSecret"] = "test-client-secret",
                ["OAuth:Google:ClientId"] = "test-client-id",
                ["OAuth:Google:ClientSecret"] = "test-client-secret",
                ["OAuth:LinkedIn:ClientId"] = "test-client-id",
                ["OAuth:LinkedIn:ClientSecret"] = "test-client-secret",
                ["OAuth:Facebook:AppId"] = "test-app-id",
                ["OAuth:Facebook:AppSecret"] = "test-app-secret"
            });
            if (_configuration is not null) config.AddInMemoryCollection(_configuration);
        });

        builder.ConfigureServices(services =>
        {
            if (_useTestAuthentication)
            {
                var initializer = services.SingleOrDefault(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType == typeof(SingleUserInitializer));
                if (initializer is not null) services.Remove(initializer);
            }

            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add DbContext with test connection string
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
                options.UseSqlite(_connectionString)
                    .AddInterceptors(
                        serviceProvider.GetRequiredService<SqliteConnectionInterceptor>(),
                        serviceProvider.GetRequiredService<GuidKeyInterceptor>()));

            if (_useTestAuthentication)
            {
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                });

                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme, _ => { });
            }

        });

        builder.UseEnvironment("Testing");
    }
}