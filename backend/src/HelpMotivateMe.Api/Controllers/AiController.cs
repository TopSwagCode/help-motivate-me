using System.Security.Claims;
using System.Text.Json;
using HelpMotivateMe.Core.Constants;
using HelpMotivateMe.Core.DTOs.Ai;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<AiController> _logger;

    public AiController(IOpenAiService openAiService, ILogger<AiController> logger)
    {
        _openAiService = openAiService;
        _logger = logger;
    }

    [HttpPost("onboarding/chat")]
    public async Task StreamChat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            var systemPrompt = OnboardingPrompts.GetPromptForStep(request.Step);

            await foreach (var chunk in _openAiService.StreamChatCompletionAsync(
                request.Messages,
                systemPrompt,
                userId,
                cancellationToken))
            {
                var json = JsonSerializer.Serialize(chunk);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat stream cancelled for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat stream for user {UserId}", userId);
            await Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpPost("onboarding/transcribe")]
    [RequestSizeLimit(25 * 1024 * 1024)] // 25MB limit for audio
    public async Task<ActionResult<TranscriptionResponse>> TranscribeAudio(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (file == null || file.Length == 0)
        {
            return BadRequest("No audio file provided");
        }

        var allowedTypes = new[] { "audio/webm", "audio/wav", "audio/mp3", "audio/mpeg", "audio/ogg", "audio/mp4" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest($"Unsupported audio format: {file.ContentType}");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _openAiService.TranscribeAudioAsync(
                stream,
                file.FileName,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio for user {UserId}", userId);
            return StatusCode(500, "Failed to transcribe audio");
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
}
