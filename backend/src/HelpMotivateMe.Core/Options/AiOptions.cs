namespace HelpMotivateMe.Core.Options;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string ChatModel { get; set; } = "gpt-4.1-mini";
    public bool EnableTranscription { get; set; } = true;
    public string TranscriptionModel { get; set; } = "whisper-1";
    public decimal InputCostPer1MTokens { get; set; } = 0.40m;
    public decimal OutputCostPer1MTokens { get; set; } = 1.60m;
    public decimal TranscriptionCostPerMinute { get; set; } = 0.006m;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ChatModel) &&
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out _);

    public bool IsTranscriptionEnabled =>
        IsEnabled && EnableTranscription && !string.IsNullOrWhiteSpace(TranscriptionModel);
}