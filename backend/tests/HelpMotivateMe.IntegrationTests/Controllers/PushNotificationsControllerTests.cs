using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.Core.DTOs.Notifications;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

public sealed class PushNotificationsControllerTests(DatabaseFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    [Fact]
    public async Task Configuration_IsPublicAndReturnsGeneratedKey()
    {
        var response = await Client.GetAsync("/api/notifications/push/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configuration = await response.Content.ReadFromJsonAsync<PushConfigurationResponse>();
        configuration!.PublicKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SubscribeStatusAndUnsubscribe_ManageCurrentUsersDevice()
    {
        var user = await DataBuilder.CreateUserAsync();
        var request = new PushSubscriptionRequest(
            "https://push.example.test/subscription-1",
            new PushSubscriptionKeys("p256dh-key", "auth-key"));

        var subscribe = await Client.PostAsJsonAsync("/api/notifications/push/subscribe", request, user.Id);
        var status = await Client.GetFromJsonAsync<PushSubscriptionsStatusResponse>(
            "/api/notifications/push/status", user.Id);
        var unsubscribe = await Client.DeleteAsync(
            $"/api/notifications/push/unsubscribe?endpoint={Uri.EscapeDataString(request.Endpoint)}", user.Id);
        var finalStatus = await Client.GetFromJsonAsync<PushSubscriptionsStatusResponse>(
            "/api/notifications/push/status", user.Id);

        subscribe.StatusCode.Should().Be(HttpStatusCode.NoContent);
        status!.HasSubscriptions.Should().BeTrue();
        status.SubscriptionCount.Should().Be(1);
        unsubscribe.StatusCode.Should().Be(HttpStatusCode.NoContent);
        finalStatus!.HasSubscriptions.Should().BeFalse();
    }

    [Fact]
    public async Task Subscribe_RequiresAuthentication()
    {
        var request = new PushSubscriptionRequest(
            "https://push.example.test/subscription-1",
            new PushSubscriptionKeys("p256dh-key", "auth-key"));

        var response = await Client.PostAsJsonAsync("/api/notifications/push/subscribe", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendTestNotification_OnlyTargetsCurrentUsersSubscriptions()
    {
        var user = await DataBuilder.CreateUserAsync();

        var response = await Client.PostAsJsonAsync<object?>(
            "/api/notifications/push/test", null, user.Id);
        var result = await response.Content.ReadFromJsonAsync<PushNotificationResult>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().Be(new PushNotificationResult(0, 0, 0));
    }
}