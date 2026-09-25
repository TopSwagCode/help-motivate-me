using System.Text.Json;
using System.Text.Json.Serialization;
using HelpMotivateMe.Core.DTOs.Ai;
using HelpMotivateMe.Core.DTOs.Identities;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Localization;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpMotivateMe.Api.Controllers;

[Route("api/ai")]
[Authorize]
public class AiController : ApiControllerBase
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IResourceAuthorizationService _auth;
    private readonly AppDbContext _db;
    private readonly AiOptions _options;
    private readonly ILogger<AiController> _logger;
    private readonly IOpenAiService _openAiService;

    public AiController(IOpenAiService openAiService, IOptions<AiOptions> options, ILogger<AiController> logger,
        AppDbContext db,
        IResourceAuthorizationService auth)
    {
        _openAiService = openAiService;
        _options = options.Value;
        _logger = logger;
        _db = db;
        _auth = auth;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public ActionResult<AiStatusResponse> GetStatus()
    {
        return Ok(new AiStatusResponse(
            _options.IsEnabled,
            _options.IsEnabled ? _options.Provider : null,
            _options.IsEnabled ? _options.ChatModel : null,
            _options.IsTranscriptionEnabled,
            _options.IsTranscriptionEnabled ? _options.TranscriptionModel : null));
    }

    [HttpPost("onboarding/chat")]
    public async Task StreamChat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (!await EnsureAiEnabledAsync(cancellationToken)) return;

        var userId = _auth.GetCurrentUserId();

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
                var json = JsonSerializer.Serialize(chunk, CamelCaseOptions);
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
        if (!_options.IsTranscriptionEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "AI transcription is not configured." });

        var userId = _auth.GetCurrentUserId();

        if (file == null || file.Length == 0) return BadRequest("No audio file provided");

        var allowedTypes = new[] { "audio/webm", "audio/wav", "audio/mp3", "audio/mpeg", "audio/ogg", "audio/mp4" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest($"Unsupported audio format: {file.ContentType}");

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
    ///     Stream AI chat for general task/goal/habit creation.
    ///     Uses intent classification with confidence scores.
    /// </summary>
    [HttpPost("general/chat")]
    public async Task StreamGeneralChat([FromBody] GeneralChatRequest request, CancellationToken cancellationToken)
    {
        if (!await EnsureAiEnabledAsync(cancellationToken)) return;

        var userId = _auth.GetCurrentUserId();

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
                var json = JsonSerializer.Serialize(chunk, CamelCaseOptions);
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
            await Response.WriteAsync(
                "data: {\"error\": \"An unexpected error occurred while processing the chat request.\"}\n\n",
                cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    ///     Get AI context data (user's identities and active goals) for enriching AI interactions.
    /// </summary>
    [HttpGet("context")]
    public async Task<ActionResult<AiContextResponse>> GetAiContext(CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "AI features are not configured." });

        var userId = _auth.GetCurrentUserId();

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

    /// <summary>
    ///     Create identity from AI recommendation.
    ///     Used when AI suggests creating a new identity for a task/goal/habit.
    /// </summary>
    [HttpPost("general/create-identity")]
    public async Task<ActionResult<IdentityResponse>> CreateIdentityFromAi(
        [FromBody] CreateIdentityFromAiRequest request,
        CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "AI features are not configured." });

        var userId = _auth.GetCurrentUserId();

        var identity = new Identity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            Color = request.Color,
            CreatedAt = DateTime.UtcNow
        };

        _db.Identities.Add(identity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new IdentityResponse(
            identity.Id,
            identity.Name,
            identity.Description,
            identity.Color,
            identity.Icon,
            0, 0, 0, 0, 0, 0, 0, 0, 0,
            identity.CreatedAt
        ));
    }

    private async Task<bool> EnsureAiEnabledAsync(CancellationToken cancellationToken)
    {
        if (_options.IsEnabled) return true;

        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await Response.WriteAsJsonAsync(new { message = "AI features are not configured." }, cancellationToken);
        return false;
    }
}