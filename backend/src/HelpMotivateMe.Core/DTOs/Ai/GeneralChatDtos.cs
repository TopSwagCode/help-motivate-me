namespace HelpMotivateMe.Core.DTOs.Ai;

/// <summary>
///     Request for general AI-powered task/goal/habit creation.
/// </summary>
public record GeneralChatRequest(
    List<ChatMessage> Messages,
    Dictionary<string, object>? Context = null
);

/// <summary>
///     Streaming chunk for general AI chat with intent classification.
/// </summary>
public record GeneralChatStreamChunk(
    string Content,
    bool IsComplete,
    AiIntentResponse? IntentData = null
);

/// <summary>
///     AI's classified intent with confidence score and preview data.
/// </summary>
public record AiIntentResponse(
    string Intent, // "create_task", "create_goal", "create_habit_stack", "clarify", "confirmed"
    decimal Confidence, // 0.0 - 1.0
    AiPreview? Preview, // Structured preview when confidence >= 0.5
    string? ClarifyingQuestion, // Question to ask when confidence < 0.85
    List<string> Actions, // Available actions: ["confirm", "edit", "cancel"]
    bool CreateNow = false // True when user confirmed and ready to create
);

/// <summary>
///     Preview data for a single entity to be created.
/// </summary>
public record AiPreview(
    string Type, // "task", "goal", "habitStack"
    Dictionary<string, object> Data
);

/// <summary>
///     Response containing user context for AI (identities and goals).
/// </summary>
public record AiContextResponse(
    List<IdentitySummary> Identities,
    List<GoalSummary> Goals
);

/// <summary>
///     Summary of an identity for AI context.
/// </summary>
public record IdentitySummary(
    Guid Id,
    string Name,
    string? Icon,
    string? Color
);

/// <summary>
///     Summary of a goal for AI context.
/// </summary>
public record GoalSummary(
    Guid Id,
    string Title
);

/// <summary>
///     Preview data for identity creation from AI recommendation.
/// </summary>
public record IdentityPreviewData(
    string Name,
    string? Description,
    string? Icon,
    string? Color,
    string? Reasoning
);

/// <summary>
///     Request to create an identity from AI recommendation.
/// </summary>
public record CreateIdentityFromAiRequest(
    string Name,
    string? Description,
    string? Icon,
    string? Color
);