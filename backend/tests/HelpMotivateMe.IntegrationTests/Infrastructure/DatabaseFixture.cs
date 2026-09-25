namespace HelpMotivateMe.IntegrationTests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly string _directory;

    public DatabaseFixture()
    {
        _directory = Path.Combine(Path.GetTempPath(), "help-motivate-me-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        ConnectionString = $"Data Source={Path.Combine(_directory, "tests.db")};Foreign Keys=True;Default Timeout=30";
    }

    public string ConnectionString { get; }

    public void Reset()
    {
        foreach (var file in Directory.EnumerateFiles(_directory)) File.Delete(file);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_directory, true);
        return Task.CompletedTask;
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}