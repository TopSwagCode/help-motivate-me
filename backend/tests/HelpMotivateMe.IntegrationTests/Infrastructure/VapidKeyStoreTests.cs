using FluentAssertions;
using HelpMotivateMe.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace HelpMotivateMe.IntegrationTests.Infrastructure;

public sealed class VapidKeyStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"hmm-vapid-{Guid.NewGuid():N}");

    [Fact]
    public void LoadOrCreate_GeneratesAndReusesPersistentKeys()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Vapid:KeyFilePath"] = Path.Combine(_directory, "vapid-keys.json")
        });

        var first = VapidKeyStore.LoadOrCreate(configuration);
        var second = VapidKeyStore.LoadOrCreate(configuration);

        first.PublicKey.Should().NotBeNullOrWhiteSpace();
        first.PrivateKey.Should().NotBeNullOrWhiteSpace();
        second.Should().Be(first);
    }

    [Fact]
    public void LoadOrCreate_PrefersCompleteManualConfiguration()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Vapid:PublicKey"] = "public-key",
            ["Vapid:PrivateKey"] = "private-key",
            ["Vapid:Subject"] = "mailto:test@example.com",
            ["Vapid:KeyFilePath"] = Path.Combine(_directory, "unused.json")
        });

        var result = VapidKeyStore.LoadOrCreate(configuration);

        result.PublicKey.Should().Be("public-key");
        result.PrivateKey.Should().Be("private-key");
        result.Subject.Should().Be("mailto:test@example.com");
        File.Exists(Path.Combine(_directory, "unused.json")).Should().BeFalse();
    }

    [Fact]
    public void LoadOrCreate_RejectsPartialManualConfiguration()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Vapid:PublicKey"] = "public-key",
            ["Vapid:KeyFilePath"] = Path.Combine(_directory, "unused.json")
        });

        var action = () => VapidKeyStore.LoadOrCreate(configuration);

        action.Should().Throw<InvalidOperationException>().WithMessage("*must either both be set or both be omitted*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}