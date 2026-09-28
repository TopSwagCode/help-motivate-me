using System.Net;
using System.Text.Json;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

public sealed class PwaManifestTests : IAsyncLifetime
{
    private string _directory = null!;
    private CustomWebApplicationFactory _factory = null!;

    public Task InitializeAsync()
    {
        _directory = Path.Combine(Path.GetTempPath(), "help-motivate-me-manifest-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        var connectionString = $"Data Source={Path.Combine(_directory, "manifest.db")};Foreign Keys=True";
        _factory = new CustomWebApplicationFactory(connectionString, configuration: new Dictionary<string, string?>
        {
            ["Pwa:Name"] = "North Star Habit Space",
            ["Pwa:ShortName"] = "North Star"
        });
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Manifest_UsesRuntimePwaIdentityAndIsNotCached()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/manifest.webmanifest");
        var manifest = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/manifest+json");
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
        manifest.RootElement.GetProperty("name").GetString().Should().Be("North Star Habit Space");
        manifest.RootElement.GetProperty("short_name").GetString().Should().Be("North Star");
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        Directory.Delete(_directory, true);
    }
}