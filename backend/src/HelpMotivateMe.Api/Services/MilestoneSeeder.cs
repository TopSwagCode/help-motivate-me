using System.Text.Json;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Services;

public class MilestoneSeeder : IHostedService
{
    private readonly ILogger<MilestoneSeeder> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MilestoneSeeder(IServiceProvider serviceProvider, ILogger<MilestoneSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var milestones = GetMilestoneDefinitions();
        var existingCodes = await db.MilestoneDefinitions
            .Select(m => m.Code)
            .ToHashSetAsync(cancellationToken);

        var newMilestones = milestones.Where(m => !existingCodes.Contains(m.Code)).ToList();

        if (newMilestones.Count > 0)
        {
            db.MilestoneDefinitions.AddRange(newMilestones);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} new milestone definitions", newMilestones.Count);
        }
        else
        {
            _logger.LogInformation("All milestone definitions already exist");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static List<MilestoneDefinition> GetMilestoneDefinitions()
    {
        return
        [
            // Login milestones
            new MilestoneDefinition
            {
                Code = "first_login",
                TitleKey = "milestones.first_login.title",
                DescriptionKey = "milestones.first_login.description",
                Icon = "🎉",
                TriggerEvent = "UserLoggedIn",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "login_count", threshold = 1 }),
                SortOrder = 1,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "logins_7",
                TitleKey = "milestones.logins_7.title",
                DescriptionKey = "milestones.logins_7.description",
                Icon = "📅",
                TriggerEvent = "UserLoggedIn",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "login_count", threshold = 7 }),
                SortOrder = 2,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "logins_30",
                TitleKey = "milestones.logins_30.title",
                DescriptionKey = "milestones.logins_30.description",
                Icon = "🌟",
                TriggerEvent = "UserLoggedIn",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "login_count", threshold = 30 }),
                SortOrder = 3,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "return_after_2_weeks",
                TitleKey = "milestones.return_after_2_weeks.title",
                DescriptionKey = "milestones.return_after_2_weeks.description",
                Icon = "🔄",
                TriggerEvent = "UserLoggedIn",
                RuleType = "return_after_gap",
                RuleData = JsonSerializer.Serialize(new { gap_days = 14 }),
                SortOrder = 4,
                IsActive = true
            },

            // Habit milestones
            new MilestoneDefinition
            {
                Code = "first_habit",
                TitleKey = "milestones.first_habit.title",
                DescriptionKey = "milestones.first_habit.description",
                Icon = "✅",
                TriggerEvent = "HabitCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_habits_completed", threshold = 1 }),
                SortOrder = 10,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "habits_10",
                TitleKey = "milestones.habits_10.title",
                DescriptionKey = "milestones.habits_10.description",
                Icon = "🔥",
                TriggerEvent = "HabitCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_habits_completed", threshold = 10 }),
                SortOrder = 11,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "habits_50",
                TitleKey = "milestones.habits_50.title",
                DescriptionKey = "milestones.habits_50.description",
                Icon = "💪",
                TriggerEvent = "HabitCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_habits_completed", threshold = 50 }),
                SortOrder = 12,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "habits_100",
                TitleKey = "milestones.habits_100.title",
                DescriptionKey = "milestones.habits_100.description",
                Icon = "🏆",
                TriggerEvent = "HabitCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_habits_completed", threshold = 100 }),
                SortOrder = 13,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "habits_5_in_week",
                TitleKey = "milestones.habits_5_in_week.title",
                DescriptionKey = "milestones.habits_5_in_week.description",
                Icon = "⚡",
                TriggerEvent = "HabitCompleted",
                RuleType = "window_count",
                RuleData = JsonSerializer.Serialize(new { count = 5, days = 7 }),
                SortOrder = 14,
                IsActive = true
            },

            // Task milestones
            new MilestoneDefinition
            {
                Code = "first_task",
                TitleKey = "milestones.first_task.title",
                DescriptionKey = "milestones.first_task.description",
                Icon = "📝",
                TriggerEvent = "TaskCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_tasks_completed", threshold = 1 }),
                SortOrder = 20,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "tasks_10",
                TitleKey = "milestones.tasks_10.title",
                DescriptionKey = "milestones.tasks_10.description",
                Icon = "📋",
                TriggerEvent = "TaskCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_tasks_completed", threshold = 10 }),
                SortOrder = 21,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "tasks_50",
                TitleKey = "milestones.tasks_50.title",
                DescriptionKey = "milestones.tasks_50.description",
                Icon = "🎯",
                TriggerEvent = "TaskCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_tasks_completed", threshold = 50 }),
                SortOrder = 22,
                IsActive = true
            },

            // Journal milestones
            new MilestoneDefinition
            {
                Code = "first_journal",
                TitleKey = "milestones.first_journal.title",
                DescriptionKey = "milestones.first_journal.description",
                Icon = "📖",
                TriggerEvent = "JournalEntryCreated",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_journal_entries", threshold = 1 }),
                SortOrder = 30,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "journals_10",
                TitleKey = "milestones.journals_10.title",
                DescriptionKey = "milestones.journals_10.description",
                Icon = "✍️",
                TriggerEvent = "JournalEntryCreated",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_journal_entries", threshold = 10 }),
                SortOrder = 31,
                IsActive = true
            },

            // Identity proof milestones
            new MilestoneDefinition
            {
                Code = "first_identity_proof",
                TitleKey = "milestones.first_identity_proof.title",
                DescriptionKey = "milestones.first_identity_proof.description",
                Icon = "🪪",
                TriggerEvent = "IdentityProofAdded",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_identity_proofs", threshold = 1 }),
                SortOrder = 40,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "identity_proofs_10",
                TitleKey = "milestones.identity_proofs_10.title",
                DescriptionKey = "milestones.identity_proofs_10.description",
                Icon = "🌱",
                TriggerEvent = "IdentityProofAdded",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_identity_proofs", threshold = 10 }),
                SortOrder = 41,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "identity_proofs_50",
                TitleKey = "milestones.identity_proofs_50.title",
                DescriptionKey = "milestones.identity_proofs_50.description",
                Icon = "🌳",
                TriggerEvent = "IdentityProofAdded",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_identity_proofs", threshold = 50 }),
                SortOrder = 42,
                IsActive = true
            },

            // Total wins milestones
            new MilestoneDefinition
            {
                Code = "wins_25",
                TitleKey = "milestones.wins_25.title",
                DescriptionKey = "milestones.wins_25.description",
                Icon = "🎖️",
                TriggerEvent = "HabitCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_wins", threshold = 25 }),
                SortOrder = 50,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "wins_100",
                TitleKey = "milestones.wins_100.title",
                DescriptionKey = "milestones.wins_100.description",
                Icon = "🥇",
                TriggerEvent = "HabitCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_wins", threshold = 100 }),
                SortOrder = 51,
                IsActive = true
            },
            new MilestoneDefinition
            {
                Code = "wins_500",
                TitleKey = "milestones.wins_500.title",
                DescriptionKey = "milestones.wins_500.description",
                Icon = "👑",
                TriggerEvent = "HabitCompleted",
                RuleType = "count",
                RuleData = JsonSerializer.Serialize(new { field = "total_wins", threshold = 500 }),
                SortOrder = 52,
                IsActive = true
            }
        ];
    }
}