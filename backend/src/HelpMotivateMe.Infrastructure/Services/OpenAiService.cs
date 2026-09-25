using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HelpMotivateMe.Core.DTOs.Ai;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Exceptions;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelpMotivateMe.Infrastructure.Services;

public class OpenAiService : IOpenAiService
{
    private readonly AiOptions _options;
    private readonly IAiBudgetService _budgetService;
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(
        HttpClient httpClient,
        AppDbContext db,
        IAiBudgetService budgetService,
        IOptions<AiOptions> options,
        ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _budgetService = budgetService;
        _logger = logger;
        _options = options.Value;

        if (_options.IsEnabled)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamChatCompletionAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureChatIsConfigured();

        // Estimate cost and check budget BEFORE making the API call
        var estimatedCost = EstimateChatCost(messages, systemPrompt);
        var budgetCheck = await _budgetService.CheckBudgetAsync(userId, estimatedCost, cancellationToken);
        if (!budgetCheck.IsAllowed)
        {
            await LogRejectedCallAsync(userId, _options.ChatModel, "chat", estimatedCost, cancellationToken);
            throw new AiBudgetExceededException(budgetCheck.DenialReason ?? "Budget limit exceeded");
        }

        var channel = Channel.CreateUnbounded<ChatStreamChunk>();
        var usageInfo = new UsageInfo { EstimatedCost = estimatedCost };

        // Start the producer task
        var producerTask =
            ProduceStreamChunksAsync(messages, systemPrompt, channel.Writer, usageInfo, cancellationToken);

        // Consume chunks from the channel
        await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken)) yield return chunk;

        // Wait for producer to complete
        await producerTask;

        // Log usage after streaming is complete (with both estimated and actual costs)
        await LogUsageAsync(userId, _options.ChatModel, usageInfo.InputTokens, usageInfo.OutputTokens, null, "chat",
            usageInfo.EstimatedCost, cancellationToken);
    }

    public async Task<TranscriptionResponse> TranscribeAudioAsync(
        Stream audioStream,
        string fileName,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureTranscriptionIsConfigured();

        // Get stream length for cost estimation, buffering only if necessary
        Stream streamToUse;
        MemoryStream? memoryStream = null;
        long audioLength;

        if (audioStream.CanSeek)
        {
            audioLength = audioStream.Length;
            streamToUse = audioStream;
        }
        else
        {
            memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream, cancellationToken);
            audioLength = memoryStream.Length;
            memoryStream.Position = 0;
            streamToUse = memoryStream;
        }

        try
        {
            // Estimate cost and check budget BEFORE making the API call
            var estimatedCost = EstimateWhisperCost(audioLength);
            var budgetCheck = await _budgetService.CheckBudgetAsync(userId, estimatedCost, cancellationToken);
            if (!budgetCheck.IsAllowed)
            {
                await LogRejectedCallAsync(userId, _options.TranscriptionModel, "transcription", estimatedCost,
                    cancellationToken);
                throw new AiBudgetExceededException(budgetCheck.DenialReason ?? "Budget limit exceeded");
            }

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(streamToUse);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(_options.TranscriptionModel), "model");
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

            // Log usage with both estimated and actual costs
            await LogUsageAsync(userId, _options.TranscriptionModel, 0, 0, estimatedDurationSeconds, "transcription",
                estimatedCost, cancellationToken);

            return new TranscriptionResponse(text, estimatedDurationSeconds);
        }
        finally
        {
            memoryStream?.Dispose();
        }
    }

    private decimal EstimateChatCost(List<ChatMessage> messages, string systemPrompt)
    {
        // Estimate input tokens: chars / 4 (use ceiling to avoid underestimation for budget checks)
        var totalChars = systemPrompt.Length + messages.Sum(m => m.Content.Length);
        var estimatedInputTokens = (int)Math.Ceiling(totalChars / 4m);
        // Estimate output tokens: typically similar to or less than input for assistant responses
        // Using 1.2x multiplier (was 3x which caused 7-10x overestimation)
        var estimatedOutputTokens = (int)Math.Ceiling(estimatedInputTokens * 1.2m);

         return estimatedInputTokens * _options.InputCostPer1MTokens / 1_000_000m +
             estimatedOutputTokens * _options.OutputCostPer1MTokens / 1_000_000m;
    }

    private decimal EstimateWhisperCost(long audioStreamLength)
    {
        // Estimate duration from stream length (~4KB/sec for webm audio)
        var estimatedDurationSeconds = audioStreamLength / 4000.0;
        var estimatedDurationMinutes = estimatedDurationSeconds / 60.0;
        return (decimal)estimatedDurationMinutes * _options.TranscriptionCostPerMinute;
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
                model = _options.ChatModel,
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
                if (chunk != null) await writer.WriteAsync(chunk, cancellationToken);
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

            if (root.TryGetProperty("type", out var typeProp)) data["type"] = typeProp.GetString() ?? "";

            if (root.TryGetProperty("data", out var dataProp))
                foreach (var property in dataProp.EnumerateObject())
                    data[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.Number => property.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null!,
                        JsonValueKind.Array => JsonSerializer.Deserialize<List<object>>(property.Value.GetRawText()) ??
                                               new List<object>(),
                        JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(
                            property.Value.GetRawText()) ?? new Dictionary<string, object>(),
                        _ => property.Value.GetRawText()
                    };

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
        decimal estimatedCost,
        CancellationToken cancellationToken)
    {
        decimal actualCost;
        if (requestType == "transcription" && audioDurationSeconds.HasValue)
            actualCost = audioDurationSeconds.Value / 60.0m * _options.TranscriptionCostPerMinute;
        else
            actualCost = inputTokens * _options.InputCostPer1MTokens / 1_000_000m +
                         outputTokens * _options.OutputCostPer1MTokens / 1_000_000m;

        var usageLog = new AiUsageLog
        {
            UserId = userId,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            AudioDurationSeconds = audioDurationSeconds,
            EstimatedCostUsd = estimatedCost,
            ActualCostUsd = actualCost,
            RequestType = requestType
        };

        _db.AiUsageLogs.Add(usageLog);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AI usage logged: User={UserId}, Model={Model}, Tokens={Input}/{Output}, EstimatedCost=${EstimatedCost:F6}, ActualCost=${ActualCost:F6}",
            userId, model, inputTokens, outputTokens, estimatedCost, actualCost);
    }

    private async Task LogRejectedCallAsync(
        Guid userId,
        string model,
        string requestType,
        decimal estimatedCost,
        CancellationToken cancellationToken)
    {
        var usageLog = new AiUsageLog
        {
            UserId = userId,
            Model = model,
            InputTokens = 0,
            OutputTokens = 0,
            AudioDurationSeconds = null,
            EstimatedCostUsd = estimatedCost,
            ActualCostUsd = 0,
            RequestType = requestType,
            Rejected = true
        };

        _db.AiUsageLogs.Add(usageLog);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "AI call rejected: User={UserId}, Model={Model}, EstimatedCost=${EstimatedCost:F6}",
            userId, model, estimatedCost);
    }

    private void EnsureChatIsConfigured()
    {
        if (!_options.IsEnabled)
            throw new InvalidOperationException("AI features are not configured.");
    }

    private void EnsureTranscriptionIsConfigured()
    {
        if (!_options.IsTranscriptionEnabled)
            throw new InvalidOperationException("AI transcription is not configured.");
    }

    private class UsageInfo
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal EstimatedCost { get; set; }
    }
}