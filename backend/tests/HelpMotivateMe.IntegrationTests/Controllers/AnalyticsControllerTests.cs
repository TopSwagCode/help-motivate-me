using System.Net.Http.Json;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class AnalyticsControllerTests : IntegrationTestBase
{
    public AnalyticsControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }


    [Fact]
    public async Task GetStreaks_ReturnsEmptyForNewUser()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<StreakSummaryResponse>("/api/analytics/streaks");

        // Assert
        response.Should().NotBeNull();
        response!.TotalHabits.Should().Be(0);
        response.Streaks.Should().BeEmpty();
    }


    [Fact]
    public async Task GetCompletionRates_CalculatesCorrectPercentage()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);

        // Create 4 tasks: 3 completed, 1 pending = 75%
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 1", TaskItemStatus.Completed);
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 2", TaskItemStatus.Completed);
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 3", TaskItemStatus.Completed);
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 4");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<CompletionRateResponse>("/api/analytics/completion-rates");

        // Assert
        response.Should().NotBeNull();
        response!.DailyRate.Should().Be(75.0);
        response.TotalCompletions.Should().Be(3);
    }

    [Fact]
    public async Task GetCompletionRates_ReturnsZeroForNoTasks()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<CompletionRateResponse>("/api/analytics/completion-rates");

        // Assert
        response.Should().NotBeNull();
        response!.DailyRate.Should().Be(0);
        response.TotalCompletions.Should().Be(0);
    }

    [Fact]
    public async Task GetCompletionRates_Returns100ForAllCompleted()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);

        await DataBuilder.CreateTaskAsync(goal.Id, "Task 1", TaskItemStatus.Completed);
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 2", TaskItemStatus.Completed);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<CompletionRateResponse>("/api/analytics/completion-rates");

        // Assert
        response!.DailyRate.Should().Be(100.0);
    }


    [Fact]
    public async Task GetHeatmap_ReturnsCorrectDaysRange()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create task completed today
        await DataBuilder.CreateTaskAsync(goal.Id, "Today task", TaskItemStatus.Completed, completedAt: today);

        // Create task completed 50 days ago (within 90 day default)
        var fiftyDaysAgo = today.AddDays(-50);
        await DataBuilder.CreateTaskAsync(goal.Id, "Old task", TaskItemStatus.Completed, completedAt: fiftyDaysAgo);

        // Create task completed 100 days ago (outside 90 day default)
        var hundredDaysAgo = today.AddDays(-100);
        await DataBuilder.CreateTaskAsync(goal.Id, "Very old task", TaskItemStatus.Completed,
            completedAt: hundredDaysAgo);

        // Act - default is 90 days
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<HeatmapEntry>>("/api/analytics/heatmap");

        // Assert
        response.Should().HaveCount(2); // Today and 50 days ago, not 100 days ago
        response.Should().Contain(h => h.Date == today);
        response.Should().Contain(h => h.Date == fiftyDaysAgo);
        response.Should().NotContain(h => h.Date == hundredDaysAgo);
    }

    [Fact]
    public async Task GetHeatmap_RespectsCustomDaysParameter()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create task completed today
        await DataBuilder.CreateTaskAsync(goal.Id, "Today task", TaskItemStatus.Completed, completedAt: today);

        // Create task completed 20 days ago
        var twentyDaysAgo = today.AddDays(-20);
        await DataBuilder.CreateTaskAsync(goal.Id, "Recent task", TaskItemStatus.Completed, completedAt: twentyDaysAgo);

        // Act - request only 10 days
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<HeatmapEntry>>("/api/analytics/heatmap?days=10");

        // Assert
        response.Should().ContainSingle(); // Only today's task
        response.Should().Contain(h => h.Date == today);
    }

    [Fact]
    public async Task GetHeatmap_GroupsByDate_CountsMultipleCompletions()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create 3 tasks completed today
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 1", TaskItemStatus.Completed, completedAt: today);
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 2", TaskItemStatus.Completed, completedAt: today);
        await DataBuilder.CreateTaskAsync(goal.Id, "Task 3", TaskItemStatus.Completed, completedAt: today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<HeatmapEntry>>("/api/analytics/heatmap");

        // Assert
        response.Should().ContainSingle();
        response![0].Date.Should().Be(today);
        response[0].Count.Should().Be(3);
    }

    [Fact]
    public async Task GetHeatmap_ExcludesPendingTasks()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create pending task (should not appear)
        await DataBuilder.CreateTaskAsync(goal.Id, "Pending task");

        // Create completed task
        await DataBuilder.CreateTaskAsync(goal.Id, "Completed task", TaskItemStatus.Completed, completedAt: today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<HeatmapEntry>>("/api/analytics/heatmap");

        // Assert
        response.Should().ContainSingle();
        response![0].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetHeatmap_ReturnsOrderedByDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);

        // Create tasks in random order
        await DataBuilder.CreateTaskAsync(goal.Id, "Today", TaskItemStatus.Completed, completedAt: today);
        await DataBuilder.CreateTaskAsync(goal.Id, "Two days ago", TaskItemStatus.Completed, completedAt: twoDaysAgo);
        await DataBuilder.CreateTaskAsync(goal.Id, "Yesterday", TaskItemStatus.Completed, completedAt: yesterday);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<HeatmapEntry>>("/api/analytics/heatmap");

        // Assert
        response.Should().HaveCount(3);
        response![0].Date.Should().Be(twoDaysAgo);
        response[1].Date.Should().Be(yesterday);
        response[2].Date.Should().Be(today);
    }


    [Fact]
    public async Task GetCompletionRates_OnlyCountsUsersTasks()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();

        var goal1 = await DataBuilder.CreateGoalAsync(user1.Id);
        var goal2 = await DataBuilder.CreateGoalAsync(user2.Id);

        // User1: 1 completed
        await DataBuilder.CreateTaskAsync(goal1.Id, "User1 Task", TaskItemStatus.Completed);

        // User2: 2 completed
        await DataBuilder.CreateTaskAsync(goal2.Id, "User2 Task 1", TaskItemStatus.Completed);
        await DataBuilder.CreateTaskAsync(goal2.Id, "User2 Task 2", TaskItemStatus.Completed);

        // Act
        Client.AuthenticateAs(user1.Id);
        var response = await Client.GetFromJsonAsync<CompletionRateResponse>("/api/analytics/completion-rates");

        // Assert
        response!.TotalCompletions.Should().Be(1); // Only user1's task
    }

    [Fact]
    public async Task GetHeatmap_OnlyCountsUsersTasks()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var goal1 = await DataBuilder.CreateGoalAsync(user1.Id);
        var goal2 = await DataBuilder.CreateGoalAsync(user2.Id);

        await DataBuilder.CreateTaskAsync(goal1.Id, "User1 Task", TaskItemStatus.Completed, completedAt: today);
        await DataBuilder.CreateTaskAsync(goal2.Id, "User2 Task", TaskItemStatus.Completed, completedAt: today);

        // Act
        Client.AuthenticateAs(user1.Id);
        var response = await Client.GetFromJsonAsync<List<HeatmapEntry>>("/api/analytics/heatmap");

        // Assert
        response.Should().ContainSingle();
        response![0].Count.Should().Be(1);
    }
}

// Response DTOs for deserialization
public record StreakSummaryResponse(
    int TotalHabits,
    int ActiveStreaks,
    int LongestActiveStreak,
    List<TaskStreakResponse> Streaks
);

public record TaskStreakResponse(
    Guid TaskId,
    string TaskTitle,
    int CurrentStreak,
    int LongestStreak,
    DateOnly? LastCompletedDate,
    bool IsOnGracePeriod,
    int DaysUntilStreakBreaks
);

public record CompletionRateResponse(
    double DailyRate,
    double WeeklyRate,
    double MonthlyRate,
    int TotalCompletions,
    int MissedDays
);

public record HeatmapEntry(
    DateOnly Date,
    int Count
);