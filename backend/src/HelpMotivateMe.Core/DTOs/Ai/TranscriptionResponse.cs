namespace HelpMotivateMe.Core.DTOs.Ai;

public record TranscriptionResponse(
    string Text,
    int DurationSeconds
);
