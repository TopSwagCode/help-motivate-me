using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

/// <summary>
/// Service for calculating identity scores based on recency-weighted votes.
/// The scoring system measures identity strength and momentum, designed to be
/// forgiving, anti-shame, and motivating.
/// </summary>
public class IdentityScoreService
{
    private readonly IQueryInterface<Identity> _identities;
    private readonly IQueryInterface<HabitStackItemCompletion> _completions;
    private readonly IQueryInterface<TaskItem> _tasks;
    private readonly IQueryInterface<IdentityProof> _proofs;

    // Maximum votes counted per identity per day to prevent gaming
    private const int MaxDailyVotes = 10;

    // Maximum evaluation window in days
    private const int MaxWindowDays = 14;

    // Daily decay factor when no actions are completed
    private const double DailyDecayFactor = 0.97;

    public IdentityScoreService(
        IQueryInterface<Identity> identities,
        IQueryInterface<HabitStackItemCompletion> completions,
        IQueryInterface<TaskItem> tasks,
        IQueryInterface<IdentityProof> proofs)
    {
        _identities = identities;
        _completions = completions;
        _tasks = tasks;
        _proofs = proofs;
    }

    /// <summary>
    /// Calculate identity scores for a user.
    /// </summary>
    public async Task<List<IdentityScoreResult>> CalculateScoresAsync(Guid userId, DateOnly targetDate)
    {
        // Calculate date range for queries - only fetch data we need
        var startDate = targetDate.AddDays(-MaxWindowDays);

        // Load identities with habit stacks and items, but filter completions at DB level
        var identities = await _identities
            .Include(i => i.HabitStacks)
                .ThenInclude(hs => hs.Items)
                    .ThenInclude(hsi => hsi.Completions.Where(c => c.CompletedDate >= startDate && c.CompletedDate <= targetDate))
            .Include(i => i.Tasks.Where(t => t.CompletedAt >= startDate && t.CompletedAt <= targetDate))
            .Include(i => i.Proofs.Where(p => p.ProofDate >= startDate && p.ProofDate <= targetDate))
            .Where(i => i.UserId == userId)
            .ToListAsync();

        // Get first action dates per identity (separate query for efficiency)
        var firstActionDates = await GetFirstActionDatesAsync(userId, identities.Select(i => i.Id).ToList());

        var results = new List<IdentityScoreResult>();

        foreach (var identity in identities)
        {
            var firstActionDate = firstActionDates.GetValueOrDefault(identity.Id);
            var score = CalculateIdentityScore(identity, targetDate, firstActionDate);
            results.Add(score);
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    private async Task<Dictionary<Guid, DateOnly?>> GetFirstActionDatesAsync(Guid userId, List<Guid> identityIds)
    {
        // Get earliest habit completion date per identity
        var habitFirstDates = await _completions
            .Where(c => c.HabitStackItem.HabitStack.UserId == userId &&
                       identityIds.Contains(c.HabitStackItem.HabitStack.IdentityId!.Value))
            .GroupBy(c => c.HabitStackItem.HabitStack.IdentityId!.Value)
            .Select(g => new { IdentityId = g.Key, FirstDate = g.Min(c => c.CompletedDate) })
            .ToDictionaryAsync(x => x.IdentityId, x => (DateOnly?)x.FirstDate);

        // Get earliest task completion date per identity
        var taskFirstDates = await _tasks
            .Where(t => t.Goal.UserId == userId &&
                       t.IdentityId.HasValue &&
                       identityIds.Contains(t.IdentityId.Value) &&
                       t.CompletedAt.HasValue)
            .GroupBy(t => t.IdentityId!.Value)
            .Select(g => new { IdentityId = g.Key, FirstDate = g.Min(t => t.CompletedAt!.Value) })
            .ToDictionaryAsync(x => x.IdentityId, x => (DateOnly?)x.FirstDate);

        // Get earliest identity proof date per identity
        var proofFirstDates = await _proofs
            .Where(p => p.UserId == userId && identityIds.Contains(p.IdentityId))
            .GroupBy(p => p.IdentityId)
            .Select(g => new { IdentityId = g.Key, FirstDate = g.Min(p => p.ProofDate) })
            .ToDictionaryAsync(x => x.IdentityId, x => (DateOnly?)x.FirstDate);

        // Merge and get earliest date for each identity
        var result = new Dictionary<Guid, DateOnly?>();
        foreach (var identityId in identityIds)
        {
            var habitDate = habitFirstDates.GetValueOrDefault(identityId);
            var taskDate = taskFirstDates.GetValueOrDefault(identityId);
            var proofDate = proofFirstDates.GetValueOrDefault(identityId);

            // Find the earliest of all three dates
            var dates = new[] { habitDate, taskDate, proofDate }.Where(d => d.HasValue).ToList();
            result[identityId] = dates.Count > 0 ? dates.Min() : null;
        }

        return result;
    }

    private IdentityScoreResult CalculateIdentityScore(Identity identity, DateOnly targetDate, DateOnly? firstActionDate)
    {
        // If no actions ever, return dormant score
        if (!firstActionDate.HasValue)
        {
            return new IdentityScoreResult(
                identity.Id,
                identity.Name,
                identity.Color,
                identity.Icon,
                0,
                IdentityStatus.Dormant,
                TrendDirection.Neutral,
                0,
                false
            );
        }

        var accountAgeDays = targetDate.DayNumber - firstActionDate.Value.DayNumber;
        
        // Dynamic time window grows with account age
        // EffectiveWindow = min(14, AccountAgeDays + 3)
        var effectiveWindow = Math.Min(MaxWindowDays, accountAgeDays + 3);
        effectiveWindow = Math.Max(1, effectiveWindow); // At least 1 day

        // Calculate raw score with recency weighting
        double rawScore = 0;
        var votesPerDay = new Dictionary<DateOnly, int>();
        
        for (int dayOffset = 0; dayOffset < effectiveWindow; dayOffset++)
        {
            var checkDate = targetDate.AddDays(-dayOffset);
            var dayVotes = GetDayVotes(identity, checkDate);
            
            // Cap daily votes
            dayVotes = Math.Min(dayVotes, MaxDailyVotes);
            votesPerDay[checkDate] = dayVotes;
            
            // Apply recency weight (1.0 for today, decreasing by 0.1 per day, min 0.1)
            var recencyWeight = Math.Max(0.1, 1.0 - (dayOffset * 0.1));
            rawScore += dayVotes * recencyWeight;
        }

        // Calculate max possible score
        var maxPossible = 0.0;
        for (int dayOffset = 0; dayOffset < effectiveWindow; dayOffset++)
        {
            var recencyWeight = Math.Max(0.1, 1.0 - (dayOffset * 0.1));
            maxPossible += MaxDailyVotes * recencyWeight;
        }

        // Normalize to 0-100
        var normalizedScore = maxPossible > 0 ? (rawScore / maxPossible) * 100 : 0;

        // Apply decay if no actions today
        var todayVotes = votesPerDay.GetValueOrDefault(targetDate, 0);
        var consecutiveInactiveDays = 0;
        
        if (todayVotes == 0)
        {
            // Count consecutive days without activity
            for (int dayOffset = 0; dayOffset < effectiveWindow; dayOffset++)
            {
                var checkDate = targetDate.AddDays(-dayOffset);
                if (votesPerDay.GetValueOrDefault(checkDate, 0) == 0)
                {
                    consecutiveInactiveDays++;
                }
                else
                {
                    break;
                }
            }
            
            // Apply decay: Score = Score × 0.97^n
            var decayFactor = Math.Pow(DailyDecayFactor, consecutiveInactiveDays);
            normalizedScore *= decayFactor;
        }

        // Apply beginner confidence floor (first 14 days)
        var isNewUser = accountAgeDays < MaxWindowDays;
        var hasRecentActivity = HasActivityInLast48Hours(identity, targetDate);
        
        if (isNewUser && hasRecentActivity)
        {
            // Floor = 30 + (AccountAgeDays × 2)
            var floor = 30 + (accountAgeDays * 2);
            normalizedScore = Math.Max(normalizedScore, floor);
        }

        // Clamp to 0-100
        normalizedScore = Math.Clamp(normalizedScore, 0, 100);

        // Calculate trend (compare last 3 days vs previous 3 days)
        var trend = CalculateTrend(identity, targetDate);

        // Determine status based on score
        var status = GetStatus(normalizedScore);

        // Show numeric score from day 1 (previously hidden for 7 days)
        var showNumericScore = true;

        return new IdentityScoreResult(
            identity.Id,
            identity.Name,
            identity.Color,
            identity.Icon,
            (int)Math.Round(normalizedScore),
            status,
            trend,
            accountAgeDays,
            showNumericScore
        );
    }

    private int GetDayVotes(Identity identity, DateOnly date)
    {
        var votes = 0;

        // Habit stack item completions (+1 per small habit)
        var habitCompletions = identity.HabitStacks
            .SelectMany(hs => hs.Items)
            .SelectMany(hsi => hsi.Completions.Where(c => c.CompletedDate == date))
            .Count();
        votes += habitCompletions;

        // Full habit stack completion bonus (+2 per fully completed stack)
        foreach (var stack in identity.HabitStacks)
        {
            if (stack.Items.Count > 0)
            {
                var itemsCompletedToday = stack.Items.Count(i => 
                    i.Completions.Any(c => c.CompletedDate == date));
                
                if (itemsCompletedToday == stack.Items.Count)
                {
                    votes += 2; // Bonus for completing full stack
                }
            }
        }

        // Task completions (+2 per regular task)
        var taskCompletions = identity.Tasks
            .Count(t => t.Status == TaskItemStatus.Completed &&
                       t.CompletedAt.HasValue &&
                       t.CompletedAt.Value == date);
        votes += taskCompletions * 2;

        // Identity proofs (+1 Easy, +2 Moderate, +3 Hard based on intensity)
        var proofVotes = identity.Proofs?
            .Where(p => p.ProofDate == date)
            .Sum(p => (int)p.Intensity) ?? 0;
        votes += proofVotes;

        return votes;
    }

    private bool HasActivityInLast48Hours(Identity identity, DateOnly targetDate)
    {
        var twoDaysAgo = targetDate.AddDays(-2);
        
        // Check habit completions
        var hasHabitActivity = identity.HabitStacks
            .SelectMany(hs => hs.Items)
            .SelectMany(hsi => hsi.Completions)
            .Any(c => c.CompletedDate >= twoDaysAgo && c.CompletedDate <= targetDate);

        if (hasHabitActivity) return true;

        // Check task completions
        var hasTaskActivity = identity.Tasks
            .Any(t => t.CompletedAt.HasValue &&
                     t.CompletedAt.Value >= twoDaysAgo &&
                     t.CompletedAt.Value <= targetDate);

        if (hasTaskActivity) return true;

        // Check identity proofs
        var hasProofActivity = identity.Proofs?
            .Any(p => p.ProofDate >= twoDaysAgo && p.ProofDate <= targetDate) ?? false;

        return hasProofActivity;
    }

    private TrendDirection CalculateTrend(Identity identity, DateOnly targetDate)
    {
        // Compare votes from last 3 days vs previous 3 days
        var recentVotes = 0;
        var previousVotes = 0;

        for (int i = 0; i < 3; i++)
        {
            recentVotes += GetDayVotes(identity, targetDate.AddDays(-i));
        }

        for (int i = 3; i < 6; i++)
        {
            previousVotes += GetDayVotes(identity, targetDate.AddDays(-i));
        }

        if (recentVotes > previousVotes + 1)
            return TrendDirection.Up;
        if (recentVotes < previousVotes - 1)
            return TrendDirection.Down;
        return TrendDirection.Neutral;
    }

    private static IdentityStatus GetStatus(double score) => score switch
    {
        >= 90 => IdentityStatus.Automatic,
        >= 75 => IdentityStatus.Strong,
        >= 60 => IdentityStatus.Stabilizing,
        >= 40 => IdentityStatus.Emerging,
        >= 25 => IdentityStatus.Forming,
        _ => IdentityStatus.Dormant
    };
}

/// <summary>
/// Result of identity score calculation.
/// </summary>
public record IdentityScoreResult(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int Score,
    IdentityStatus Status,
    TrendDirection Trend,
    int AccountAgeDays,
    bool ShowNumericScore
);

/// <summary>
/// Identity status labels based on score.
/// </summary>
public enum IdentityStatus
{
    Dormant,      // 0-24
    Forming,      // 25-39
    Emerging,     // 40-59
    Stabilizing,  // 60-74
    Strong,       // 75-89
    Automatic     // 90-100
}

/// <summary>
/// Trend direction for identity momentum.
/// </summary>
public enum TrendDirection
{
    Up,
    Down,
    Neutral
}
