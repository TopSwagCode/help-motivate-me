using System.Security.Claims;
using System.Text.Json;
using HelpMotivateMe.Core.DTOs.Ai;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Localization;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<AiController> _logger;
    private readonly AppDbContext _db;

    public AiController(IOpenAiService openAiService, ILogger<AiController> logger, AppDbContext db)
    {
        _openAiService = openAiService;
        _logger = logger;
        _db = db;
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
            // Get user's preferred language
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            var language = user?.PreferredLanguage ?? Language.English;

            var systemPrompt = LocalizedPrompts.BuildSystemPrompt(request.Step, language, request.Context);

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

    /// <summary>
    /// Stream AI chat for general task/goal/habit creation.
    /// Uses intent classification with confidence scores.
    /// </summary>
    [HttpPost("general/chat")]
    public async Task StreamGeneralChat([FromBody] GeneralChatRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            // Get user's preferred language
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            var language = user?.PreferredLanguage ?? Language.English;

            // Get user's identities for context
            var identities = await _db.Identities
                .Where(i => i.UserId == userId)
                .Select(i => new { i.Id, i.Name, i.Icon, i.Description })
                .ToListAsync(cancellationToken);

            // Build context with identities
            var context = request.Context ?? new Dictionary<string, object>();
            context["identities"] = identities;

            var systemPrompt = LocalizedPrompts.BuildGeneralCreationPrompt(language, context);

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
            _logger.LogInformation("General chat stream cancelled for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in general chat stream for user {UserId}", userId);
            await Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Get AI context data (user's identities and active goals) for enriching AI interactions.
    /// </summary>
    [HttpGet("context")]
    public async Task<ActionResult<AiContextResponse>> GetAiContext(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var identities = await _db.Identities
            .Where(i => i.UserId == userId)
            .Select(i => new IdentitySummary(i.Id, i.Name, i.Icon, i.Color))
            .ToListAsync(cancellationToken);

        var goals = await _db.Goals
            .Where(g => g.UserId == userId && !g.IsCompleted)
            .Select(g => new GoalSummary(g.Id, g.Title))
            .ToListAsync(cancellationToken);

        return Ok(new AiContextResponse(identities, goals));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
}
