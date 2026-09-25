using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.Core.DTOs.Auth;
using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HelpMotivateMe.IntegrationTests.Controllers;

public sealed class AuthControllerTests : IAsyncLifetime
{
    private string _directory = null!;
    private CustomWebApplicationFactory _factory = null!;

    public Task InitializeAsync()
    {
        _directory = Path.Combine(Path.GetTempPath(), "help-motivate-me-auth-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        var connectionString = $"Data Source={Path.Combine(_directory, "auth.db")};Foreign Keys=True;Default Timeout=30";
        _factory = new CustomWebApplicationFactory(connectionString, useTestAuthentication: false);
        _ = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        Directory.Delete(_directory, true);
    }

    [Fact]
    public async Task Startup_ProvisionsExactlyOneConfiguredUser()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var users = await db.Users.ToListAsync();

        users.Should().ContainSingle();
        users[0].Username.Should().Be("test-admin");
        users[0].PasswordHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithConfiguredCredentials_CreatesSession()
    {
        using var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-admin", "test-password-only"));
        var current = await client.GetAsync("/api/auth/me");

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        current.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await current.Content.ReadFromJsonAsync<UserResponse>();
        user!.Username.Should().Be("test-admin");
    }

    [Theory]
    [InlineData("test-admin", "wrong-password")]
    [InlineData("missing-user", "test-password-only")]
    public async Task Login_WithInvalidCredentials_UsesGenericError(string username, string password)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task RegistrationEndpoint_DoesNotExist()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnonymousPrivateRequest_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/goals");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
