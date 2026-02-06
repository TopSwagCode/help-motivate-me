using System.Text.Json;
using HelpMotivateMe.Core.DTOs.Milestones;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

public class MilestoneService : IMilestoneService
{
    private readonly AppDbContext _db;

    public MilestoneService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserMilestoneResponse>> RecordEventAsync(Guid userId, string eventType,
        object? metadata = null)
    {
        // 1. Insert domain event
        var domainEvent = new DomainEvent
        {
            UserId = userId,
            EventType = eventType,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            OccurredAt = DateTime.UtcNow
        };
        _db.DomainEvents.Add(domainEvent);

        // 2. Get or create user stats
        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats == null)
        {
            stats = new UserStats { UserId = userId };
            _db.UserStats.Add(stats);
        }

        // 3. Update stats based on event type
        UpdateStats(stats, eventType);
        stats.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // 4. Evaluate milestones
        var newlyAwarded = await EvaluateMilestonesAsync(userId, eventType, stats);

        return newlyAwarded;
    }

    public async Task<List<UserMilestoneResponse>> GetUserMilestonesAsync(Guid userId)
    {
        return await _db.UserMilestones
            .Include(um => um.MilestoneDefinition)
            .Where(um => um.UserId == userId)
            .OrderByDescending(um => um.AwardedAt)
            .Select(um => ToResponse(um))
            .ToListAsync();
    }

    public async Task<List<UserMilestoneResponse>> GetUnseenMilestonesAsync(Guid userId)
    {
        return await _db.UserMilestones
            .Include(um => um.MilestoneDefinition)
            .Where(um => um.UserId == userId && !um.HasBeenSeen)
            .OrderBy(um => um.AwardedAt)
            .Select(um => ToResponse(um))
            .ToListAsync();
    }

    public async Task MarkMilestonesSeenAsync(Guid userId, List<Guid> milestoneIds)
    {
        await _db.UserMilestones
            .Where(um => um.UserId == userId && milestoneIds.Contains(um.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(um => um.HasBeenSeen, true));
    }

    public async Task<List<MilestoneDefinitionResponse>> GetAllDefinitionsAsync()
    {
        return await _db.MilestoneDefinitions
            .OrderBy(m => m.SortOrder)
            .Select(m => new MilestoneDefinitionResponse(
                m.Id,
                m.Code,
                m.TitleKey,
                m.DescriptionKey,
                m.Icon,
                m.TriggerEvent,
                m.RuleType,
                m.RuleData,
                m.AnimationType,
                m.AnimationData,
                m.SortOrder,
                m.IsActive
            ))
            .ToListAsync();
    }

    public async Task<UserStatsResponse> GetUserStatsAsync(Guid userId)
    {
        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats == null) return new UserStatsResponse(0, 0, 0, 0, 0, 0, null, null);

        return new UserStatsResponse(
            stats.LoginCount,
            stats.TotalWins,
            stats.TotalHabitsCompleted,
            stats.TotalTasksCompleted,
            stats.TotalIdentityProofs,
            stats.TotalJournalEntries,
            stats.LastLoginAt,
            stats.LastActivityAt
        );
    }

    public async Task<MilestoneDefinitionResponse> CreateDefinitionAsync(CreateMilestoneRequest request)
    {
        var definition = new MilestoneDefinition
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            TitleKey = request.TitleKey,
            DescriptionKey = request.DescriptionKey,
            Icon = request.Icon,
            TriggerEvent = request.TriggerEvent,
            RuleType = request.RuleType,
            RuleData = request.RuleData,
            AnimationType = request.AnimationType,
            AnimationData = request.AnimationData,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.MilestoneDefinitions.Add(definition);
        await _db.SaveChangesAsync();

        return new MilestoneDefinitionResponse(
            definition.Id,
            definition.Code,
            definition.TitleKey,
            definition.DescriptionKey,
            definition.Icon,
            definition.TriggerEvent,
            definition.RuleType,
            definition.RuleData,
            definition.AnimationType,
            definition.AnimationData,
            definition.SortOrder,
            definition.IsActive
        );
    }

    public async Task<MilestoneDefinitionResponse?> UpdateDefinitionAsync(Guid id, UpdateMilestoneRequest request)
    {
        var definition = await _db.MilestoneDefinitions
            .FirstOrDefaultAsync(m => m.Id == id);
        if (definition == null)
            return null;

        definition.Code = request.Code;
        definition.TitleKey = request.TitleKey;
        definition.DescriptionKey = request.DescriptionKey;
        definition.Icon = request.Icon;
        definition.TriggerEvent = request.TriggerEvent;
        definition.RuleType = request.RuleType;
        definition.RuleData = request.RuleData;
        definition.AnimationType = request.AnimationType;
        definition.AnimationData = request.AnimationData;
        definition.SortOrder = request.SortOrder;
        definition.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return new MilestoneDefinitionResponse(
            definition.Id,
            definition.Code,
            definition.TitleKey,
            definition.DescriptionKey,
            definition.Icon,
            definition.TriggerEvent,
            definition.RuleType,
            definition.RuleData,
            definition.AnimationType,
            definition.AnimationData,
            definition.SortOrder,
            definition.IsActive
        );
    }

    public async Task<bool> ToggleDefinitionAsync(Guid id, bool isActive)
    {
        var definition = await _db.MilestoneDefinitions.FindAsync(id);
        if (definition == null)
            return false;

        definition.IsActive = isActive;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDefinitionAsync(Guid id)
    {
        var definition = await _db.MilestoneDefinitions.FindAsync(id);
        if (definition == null)
            return false;

        // Also delete any user milestones associated with this definition
        var userMilestones = await _db.UserMilestones
            .Where(um => um.MilestoneDefinitionId == id)
            .ToListAsync();

        _db.UserMilestones.RemoveRange(userMilestones);
        _db.MilestoneDefinitions.Remove(definition);
        await _db.SaveChangesAsync();
        return true;
    }

    private void UpdateStats(UserStats stats, string eventType)
    {
        stats.LastActivityAt = DateTime.UtcNow;

        switch (eventType)
        {
            case "UserLoggedIn":
                stats.PreviousLoginAt = stats.LastLoginAt;
                stats.LastLoginAt = DateTime.UtcNow;
                stats.LoginCount++;
                break;
            case "HabitCompleted":
                stats.TotalHabitsCompleted++;
                stats.TotalWins++;
                break;
            case "TaskCompleted":
                stats.TotalTasksCompleted++;
                stats.TotalWins++;
                break;
            case "IdentityProofAdded":
                stats.TotalIdentityProofs++;
                stats.TotalWins++;
                break;
            case "JournalEntryCreated":
                stats.TotalJournalEntries++;
                stats.TotalWins++;
                break;
        }
    }

    private async Task<List<UserMilestoneResponse>> EvaluateMilestonesAsync(Guid userId, string eventType,
        UserStats stats)
    {
        // Get milestone definitions triggered by this event
        var definitions = await _db.MilestoneDefinitions
            .Where(m => m.IsActive && m.TriggerEvent == eventType)
            .ToListAsync();

        // Get already awarded milestones for this user
        var awardedDefinitionIds = await _db.UserMilestones
            .Where(um => um.UserId == userId)
            .Select(um => um.MilestoneDefinitionId)
            .ToHashSetAsync();

        var newlyAwarded = new List<UserMilestoneResponse>();

        foreach (var definition in definitions)
        {
            // Skip if already awarded
            if (awardedDefinitionIds.Contains(definition.Id))
                continue;

            // Evaluate rule
            if (await EvaluateRuleAsync(userId, stats, definition))
            {
                var userMilestone = new UserMilestone
                {
                    UserId = userId,
                    MilestoneDefinitionId = definition.Id,
                    AwardedAt = DateTime.UtcNow,
                    HasBeenSeen = false
                };
                _db.UserMilestones.Add(userMilestone);

                // Need to save to get the ID
                await _db.SaveChangesAsync();

                newlyAwarded.Add(new UserMilestoneResponse(
                    userMilestone.Id,
                    definition.Id,
                    definition.Code,
                    definition.TitleKey,
                    definition.DescriptionKey,
                    definition.Icon,
                    definition.AnimationType,
                    definition.AnimationData,
                    userMilestone.AwardedAt,
                    userMilestone.HasBeenSeen
                ));
            }
        }

        return newlyAwarded;
    }

    private async Task<bool> EvaluateRuleAsync(Guid userId, UserStats stats, MilestoneDefinition definition)
    {
        var ruleData = JsonSerializer.Deserialize<JsonElement>(definition.RuleData);

        return definition.RuleType switch
        {
            "count" => EvaluateCountRule(stats, ruleData),
            "window_count" => await EvaluateWindowCountRuleAsync(userId, definition.TriggerEvent, ruleData),
            "return_after_gap" => EvaluateReturnAfterGapRule(stats, ruleData),
            _ => false
        };
    }

    private bool EvaluateCountRule(UserStats stats, JsonElement ruleData)
    {
        var field = ruleData.GetProperty("field").GetString();
        var threshold = ruleData.GetProperty("threshold").GetInt32();

        var value = field switch
        {
            "login_count" => stats.LoginCount,
            "total_wins" => stats.TotalWins,
            "total_habits_completed" => stats.TotalHabitsCompleted,
            "total_tasks_completed" => stats.TotalTasksCompleted,
            "total_identity_proofs" => stats.TotalIdentityProofs,
            "total_journal_entries" => stats.TotalJournalEntries,
            _ => 0
        };

        return value >= threshold;
    }

    private async Task<bool> EvaluateWindowCountRuleAsync(Guid userId, string eventType, JsonElement ruleData)
    {
        var count = ruleData.GetProperty("count").GetInt32();
        var days = ruleData.GetProperty("days").GetInt32();

        var windowStart = DateTime.UtcNow.AddDays(-days);
        var eventCount = await _db.DomainEvents
            .CountAsync(e => e.UserId == userId && e.EventType == eventType && e.OccurredAt >= windowStart);

        return eventCount >= count;
    }

    private bool EvaluateReturnAfterGapRule(UserStats stats, JsonElement ruleData)
    {
        var gapDays = ruleData.GetProperty("gap_days").GetInt32();

        if (stats.LastLoginAt == null || stats.PreviousLoginAt == null)
            return false;

        var gap = stats.LastLoginAt.Value - stats.PreviousLoginAt.Value;
        return gap.TotalDays >= gapDays;
    }

    private static UserMilestoneResponse ToResponse(UserMilestone um)
    {
        return new UserMilestoneResponse(
            um.Id,
            um.MilestoneDefinitionId,
            um.MilestoneDefinition.Code,
            um.MilestoneDefinition.TitleKey,
            um.MilestoneDefinition.DescriptionKey,
            um.MilestoneDefinition.Icon,
            um.MilestoneDefinition.AnimationType,
            um.MilestoneDefinition.AnimationData,
            um.AwardedAt,
            um.HasBeenSeen
        );
    }
}