using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HelpMotivateMe.IntegrationTests.Infrastructure;

[Collection("Database")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture DbFixture;
    private IServiceScope _scope = null!;
    protected HttpClient Client = null!;
    protected TestDataBuilder DataBuilder = null!;
    protected AppDbContext Db = null!;
    protected CustomWebApplicationFactory Factory = null!;

    protected IntegrationTestBase(DatabaseFixture dbFixture)
    {
        DbFixture = dbFixture;
    }

    public async Task InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory(DbFixture.ConnectionString);
        Client = Factory.CreateClient();

        _scope = Factory.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DataBuilder = new TestDataBuilder(Db);

        // Clear data before each test
        await DataBuilder.ClearAllDataAsync();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await Factory.DisposeAsync();
    }
}