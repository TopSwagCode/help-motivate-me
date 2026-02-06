using HelpMotivateMe.Core.DTOs.Ai;

namespace HelpMotivateMe.Core.Interfaces;

public interface IOpenAiService
{
    IAsyncEnumerable<ChatStreamChunk> StreamChatCompletionAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TranscriptionResponse> TranscribeAudioAsync(
        Stream audioStream,
        string fileName,
        Guid userId,
        CancellationToken cancellationToken = default);
}