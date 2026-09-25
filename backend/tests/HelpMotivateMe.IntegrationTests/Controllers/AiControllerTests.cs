using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.Core.DTOs.Ai;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class AiControllerTests : IntegrationTestBase
{
    public AiControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }

    [Fact]
    public async Task GetStatus_WithoutApiKey_ReturnsDisabled()
    {
        var response = await Client.GetFromJsonAsync<AiStatusResponse>("/api/ai/status");

        response.Should().NotBeNull();
        response!.IsEnabled.Should().BeFalse();
        response.Provider.Should().BeNull();
        response.ChatModel.Should().BeNull();
        response.IsTranscriptionEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GeneralChat_WithoutApiKey_ReturnsServiceUnavailable()
    {
        var user = await DataBuilder.CreateUserAsync();
        Client.AuthenticateAs(user.Id);

        var response = await Client.PostAsJsonAsync("/api/ai/general/chat", new
        {
            Messages = Array.Empty<object>(),
            Context = new Dictionary<string, object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetStatus_WithCustomProvider_ReturnsConfiguredModels()
    {
        await using var factory = new CustomWebApplicationFactory(
            DbFixture.ConnectionString,
            configuration: new Dictionary<string, string?>
            {
                ["AI:Provider"] = "ExampleVendor",
                ["AI:ApiKey"] = "test-key",
                ["AI:BaseUrl"] = "https://ai.example.test/v1/",
                ["AI:ChatModel"] = "example-chat-model",
                ["AI:EnableTranscription"] = "false"
            });
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<AiStatusResponse>("/api/ai/status");

        response.Should().NotBeNull();
        response!.IsEnabled.Should().BeTrue();
        response.Provider.Should().Be("ExampleVendor");
        response.ChatModel.Should().Be("example-chat-model");
        response.IsTranscriptionEnabled.Should().BeFalse();
        response.TranscriptionModel.Should().BeNull();
    }
}