namespace HelpMotivateMe.Core.DTOs.Ai;

public record AiStatusResponse(
    bool IsEnabled,
    string? Provider,
    string? ChatModel,
    bool IsTranscriptionEnabled,
    string? TranscriptionModel
);