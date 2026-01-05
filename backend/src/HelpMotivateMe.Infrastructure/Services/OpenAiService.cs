using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HelpMotivateMe.Core.DTOs.Ai;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HelpMotivateMe.Infrastructure.Services;

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly ILogger<OpenAiService> _logger;
    private readonly string _apiKey;

    private const string ChatModel = "gpt-4.1-mini";
    private const string WhisperModel = "whisper-1";

    // Pricing per 1M tokens (USD)
    private const decimal InputCostPer1M = 0.40m;
    private const decimal OutputCostPer1M = 1.60m;
    // Whisper pricing per minute
    private const decimal WhisperCostPerMinute = 0.006m;

    public OpenAiService(
        HttpClient httpClient,
        AppDbContext db,
        IConfiguration configuration,
        ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;
        _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamChatCompletionAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<ChatStreamChunk>();
        var usageInfo = new UsageInfo();

        // Start the producer task
        var producerTask = ProduceStreamChunksAsync(messages, systemPrompt, channel.Writer, usageInfo, cancellationToken);

        // Consume chunks from the channel
        await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return chunk;
        }

        // Wait for producer to complete
        await producerTask;

        // Log usage after streaming is complete
        await LogUsageAsync(userId, ChatModel, usageInfo.InputTokens, usageInfo.OutputTokens, null, "chat", cancellationToken);
    }

    private async Task ProduceStreamChunksAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        ChannelWriter<ChatStreamChunk> writer,
        UsageInfo usageInfo,
        CancellationToken cancellationToken)
    {
        try
        {
            var allMessages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };
            allMessages.AddRange(messages.Select(m => new { role = m.Role, content = m.Content }));

            var requestBody = new
            {
                model = ChatModel,
                messages = allMessages,
                stream = true,
                stream_options = new { include_usage = true }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var fullContent = new StringBuilder();

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                var chunk = ParseStreamChunk(data, fullContent, usageInfo);
                if (chunk != null)
                {
                    await writer.WriteAsync(chunk, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat stream");
            throw;
        }
        finally
        {
            writer.Complete();
        }
    }

    private ChatStreamChunk? ParseStreamChunk(string data, StringBuilder fullContent, UsageInfo usageInfo)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            // Check for usage info (comes at the end with stream_options)
            if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
            {
                if (usage.TryGetProperty("prompt_tokens", out var promptTokens))
                    usageInfo.InputTokens = promptTokens.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var completionTokens))
                    usageInfo.OutputTokens = completionTokens.GetInt32();
            }

            // Get content delta
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];

                // Check for finish reason
                if (choice.TryGetProperty("finish_reason", out var finishReason) &&
                    finishReason.ValueKind == JsonValueKind.String &&
                    finishReason.GetString() == "stop")
                {
                    var finalExtractedData = TryExtractJsonData(fullContent.ToString());
                    return new ChatStreamChunk("", true, finalExtractedData);
                }

                if (choice.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var content))
                {
                    var contentText = content.GetString();
                    if (!string.IsNullOrEmpty(contentText))
                    {
                        fullContent.Append(contentText);

                        // Check if we have extracted data in the accumulated content
                        var extractedData = TryExtractJsonData(fullContent.ToString());

                        return new ChatStreamChunk(contentText, false, extractedData);
                    }
                }
            }

            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
            return null;
        }
    }

    public async Task<TranscriptionResponse> TranscribeAudioAsync(
        Stream audioStream,
        string fileName,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(audioStream);

        streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(WhisperModel), "model");
        content.Add(new StringContent("json"), "response_format");

        var response = await _httpClient.PostAsync("audio/transcriptions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var text = doc.RootElement.GetProperty("text").GetString() ?? "";

        // Estimate duration from audio (rough estimate based on typical speech rate)
        // OpenAI doesn't return duration, so we estimate ~150 words per minute average
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedDurationSeconds = (int)Math.Ceiling(wordCount / 2.5); // ~150 wpm = 2.5 words/sec

        // Log usage
        await LogUsageAsync(userId, WhisperModel, 0, 0, estimatedDurationSeconds, "transcription", cancellationToken);

        return new TranscriptionResponse(text, estimatedDurationSeconds);
    }

    private ExtractedData? TryExtractJsonData(string content)
    {
        // Look for JSON blocks in the content
        var jsonStart = content.IndexOf("```json");
        if (jsonStart == -1) return null;

        var jsonContentStart = content.IndexOf('\n', jsonStart);
        if (jsonContentStart == -1) return null;

        var jsonEnd = content.IndexOf("```", jsonContentStart);
        if (jsonEnd == -1) return null;

        var jsonString = content[(jsonContentStart + 1)..jsonEnd].Trim();

        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            if (!root.TryGetProperty("action", out var actionProp)) return null;

            var action = actionProp.GetString() ?? "";
            var data = new Dictionary<string, object>();

            if (root.TryGetProperty("type", out var typeProp))
            {
                data["type"] = typeProp.GetString() ?? "";
            }

            if (root.TryGetProperty("data", out var dataProp))
            {
                foreach (var property in dataProp.EnumerateObject())
                {
                    data[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.Number => property.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null!,
                        JsonValueKind.Array => JsonSerializer.Deserialize<List<object>>(property.Value.GetRawText()) ?? new List<object>(),
                        JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(property.Value.GetRawText()) ?? new Dictionary<string, object>(),
                        _ => property.Value.GetRawText()
                    };
                }
            }

            return new ExtractedData(action, data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task LogUsageAsync(
        Guid userId,
        string model,
        int inputTokens,
        int outputTokens,
        int? audioDurationSeconds,
        string requestType,
        CancellationToken cancellationToken)
    {
        decimal cost;
        if (requestType == "transcription" && audioDurationSeconds.HasValue)
        {
            cost = (audioDurationSeconds.Value / 60.0m) * WhisperCostPerMinute;
        }
        else
        {
            cost = (inputTokens * InputCostPer1M / 1_000_000m) +
                   (outputTokens * OutputCostPer1M / 1_000_000m);
        }

        var usageLog = new AiUsageLog
        {
            UserId = userId,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            AudioDurationSeconds = audioDurationSeconds,
            EstimatedCostUsd = cost,
            RequestType = requestType
        };

        _db.AiUsageLogs.Add(usageLog);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AI usage logged: User={UserId}, Model={Model}, Tokens={Input}/{Output}, Cost=${Cost:F6}",
            userId, model, inputTokens, outputTokens, cost);
    }

    private class UsageInfo
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}
