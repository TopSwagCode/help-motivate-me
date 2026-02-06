namespace HelpMotivateMe.Core.DTOs.Ai;

public record ChatRequest(
    List<ChatMessage> Messages,
    string Step,
    Dictionary<string, object>? Context = null
);

public record ChatMessage(
    string Role,
    string Content
);

public record ChatStreamChunk(
    string Content,
    bool IsComplete,
    ExtractedData? ExtractedData = null
);

public record ExtractedData(
    string Action,
    Dictionary<string, object> Data
);