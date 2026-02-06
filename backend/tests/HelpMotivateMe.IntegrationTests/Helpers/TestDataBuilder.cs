using Bogus;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Infrastructure.Data;

namespace HelpMotivateMe.IntegrationTests.Helpers;

public class TestDataBuilder
{
    private readonly AppDbContext _db;
    private readonly Faker _faker = new();

    public TestDataBuilder(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User> CreateUserAsync(string? email = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? _faker.Internet.Email(),
            DisplayName = _faker.Name.FullName(),
            IsActive = true,
            IsEmailVerified = true, // Verified for testing
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<Goal> CreateGoalAsync(
        Guid userId,
        string? title = null,
        bool isCompleted = false,
        int sortOrder = 0)
    {
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title ?? _faker.Lorem.Sentence(3),
            Description = _faker.Lorem.Paragraph(),
            IsCompleted = isCompleted,
            CompletedAt = isCompleted ? DateTime.UtcNow : null,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();
        return goal;
    }

    public async Task<TaskItem> CreateTaskAsync(
        Guid goalId,
        string? title = null,
        TaskItemStatus status = TaskItemStatus.Pending,
        DateOnly? dueDate = null,
        DateOnly? completedAt = null,
        int sortOrder = 0,
        Guid? identityId = null)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            Title = title ?? _faker.Lorem.Sentence(3),
            Description = _faker.Lorem.Paragraph(),
            Status = status,
            DueDate = dueDate,
            CompletedAt = completedAt,
            SortOrder = sortOrder,
            IdentityId = identityId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem> CreateRepeatableTaskAsync(
        Guid goalId,
        RepeatFrequency frequency,
        int intervalValue = 1,
        DateOnly? startDate = null)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            GoalId = goalId,
            Title = _faker.Lorem.Sentence(3),
            Status = TaskItemStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<Identity> CreateIdentityAsync(Guid userId, string? name = null)
    {
        var identity = new Identity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name ?? $"I am a {_faker.Name.JobTitle()}",
            Description = _faker.Lorem.Sentence(),
            Color = _faker.Internet.Color(),
            Icon = _faker.PickRandom("🎯", "💪", "📚", "🏃", "✍️", "🧘"),
            CreatedAt = DateTime.UtcNow
        };
        _db.Identities.Add(identity);
        await _db.SaveChangesAsync();
        return identity;
    }

    public async Task<HabitStack> CreateHabitStackAsync(
        Guid userId,
        string? name = null,
        bool isActive = true,
        int sortOrder = 0,
        Guid? identityId = null)
    {
        var stack = new HabitStack
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name ?? $"{_faker.Hacker.Verb()} Routine",
            Description = _faker.Lorem.Sentence(),
            IdentityId = identityId,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
        _db.HabitStacks.Add(stack);
        await _db.SaveChangesAsync();
        return stack;
    }

    public async Task<HabitStackItem> CreateHabitStackItemAsync(
        Guid habitStackId,
        string? cueDescription = null,
        string? habitDescription = null,
        int sortOrder = 0)
    {
        var item = new HabitStackItem
        {
            Id = Guid.NewGuid(),
            HabitStackId = habitStackId,
            CueDescription = cueDescription ?? _faker.Lorem.Sentence(3),
            HabitDescription = habitDescription ?? _faker.Lorem.Sentence(3),
            SortOrder = sortOrder,
            CurrentStreak = 0,
            LongestStreak = 0,
            CreatedAt = DateTime.UtcNow
        };
        _db.HabitStackItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<HabitStackItemCompletion> CreateHabitStackItemCompletionAsync(
        Guid habitStackItemId,
        DateOnly completedDate)
    {
        var completion = new HabitStackItemCompletion
        {
            Id = Guid.NewGuid(),
            HabitStackItemId = habitStackItemId,
            CompletedDate = completedDate,
            CompletedAt = DateTime.UtcNow
        };
        _db.HabitStackItemCompletions.Add(completion);
        await _db.SaveChangesAsync();
        return completion;
    }

    public async Task ClearAllDataAsync()
    {
        _db.HabitStackItemCompletions.RemoveRange(_db.HabitStackItemCompletions);
        _db.HabitStackItems.RemoveRange(_db.HabitStackItems);
        _db.HabitStacks.RemoveRange(_db.HabitStacks);
        _db.TaskItems.RemoveRange(_db.TaskItems);
        _db.Goals.RemoveRange(_db.Goals);
        _db.Identities.RemoveRange(_db.Identities);
        _db.UserExternalLogins.RemoveRange(_db.UserExternalLogins);
        _db.Users.RemoveRange(_db.Users);
        await _db.SaveChangesAsync();
    }
}