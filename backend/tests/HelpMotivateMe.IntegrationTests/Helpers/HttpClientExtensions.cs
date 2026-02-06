using System.Net.Http.Json;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Helpers;

public static class HttpClientExtensions
{
    public static HttpClient AuthenticateAs(this HttpClient client, Guid userId, string? email = null)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.EmailHeader);

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

        if (email != null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);

        return client;
    }

    public static HttpClient ClearAuthentication(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.EmailHeader);
        return client;
    }

    public static async Task<T?> GetFromJsonAsync<T>(this HttpClient client, string requestUri, Guid userId)
    {
        client.AuthenticateAs(userId);
        return await client.GetFromJsonAsync<T>(requestUri);
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T value, Guid userId)
    {
        client.AuthenticateAs(userId);
        return await client.PostAsJsonAsync(requestUri, value);
    }

    public static async Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T value, Guid userId)
    {
        client.AuthenticateAs(userId);
        return await client.PutAsJsonAsync(requestUri, value);
    }

    public static async Task<HttpResponseMessage> DeleteAsync(this HttpClient client, string requestUri, Guid userId)
    {
        client.AuthenticateAs(userId);
        return await client.DeleteAsync(requestUri);
    }

    public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, Guid userId, HttpContent? content = null)
    {
        client.AuthenticateAs(userId);
        return await client.PatchAsync(requestUri, content ?? new StringContent(""));
    }
}
