using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class TodayControllerTests : IntegrationTestBase
{
    public TodayControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }


    [Fact]
    public async Task GetToday_Task_DueIn7Days_ShouldAppearInUpcoming()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueIn7Days = today.AddDays(7);

        await DataBuilder.CreateTaskAsync(goal.Id, "Task due in 7 days", dueDate: dueIn7Days);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.UpcomingTasks.Should().ContainSingle(t => t.Title == "Task due in 7 days");
    }

    [Fact]
    public async Task GetToday_Task_DueIn8Days_ShouldNotAppearInUpcoming()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueIn8Days = today.AddDays(8);

        await DataBuilder.CreateTaskAsync(goal.Id, "Task due in 8 days", dueDate: dueIn8Days);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.UpcomingTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetToday_Task_NoDueDate_ShouldAppearInUpcoming()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);

        await DataBuilder.CreateTaskAsync(goal.Id, "Task without due date", dueDate: null);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.UpcomingTasks.Should().ContainSingle(t => t.Title == "Task without due date");
    }

    [Fact]
    public async Task GetToday_Task_FromCompletedGoal_ShouldNotAppear()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var completedGoal = await DataBuilder.CreateGoalAsync(user.Id, isCompleted: true);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await DataBuilder.CreateTaskAsync(completedGoal.Id, "Task from completed goal", dueDate: today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.UpcomingTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetToday_CompletedTask_CompletedToday_ShouldAppearInCompleted()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await DataBuilder.CreateTaskAsync(
            goal.Id,
            "Task completed today",
            TaskItemStatus.Completed,
            completedAt: today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.CompletedTasks.Should().ContainSingle(t => t.Title == "Task completed today");
    }

    [Fact]
    public async Task GetToday_CompletedTask_CompletedYesterday_ShouldNotAppearInCompletedForToday()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        await DataBuilder.CreateTaskAsync(
            goal.Id,
            "Task completed yesterday",
            TaskItemStatus.Completed,
            completedAt: yesterday);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.CompletedTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetToday_WithDateParameter_ReturnsTasksForSpecifiedDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));

        await DataBuilder.CreateTaskAsync(
            goal.Id,
            "Task completed 3 days ago",
            TaskItemStatus.Completed,
            completedAt: targetDate);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>($"/api/today?date={targetDate:yyyy-MM-dd}");

        // Assert
        response.Should().NotBeNull();
        response!.CompletedTasks.Should().ContainSingle(t => t.Title == "Task completed 3 days ago");
        response.Date.Should().Be(targetDate);
    }

    [Fact]
    public async Task GetToday_CompletedTask_AlreadyCompleted_ShouldNotAppearInUpcoming()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await DataBuilder.CreateTaskAsync(
            goal.Id,
            "Already completed task",
            TaskItemStatus.Completed,
            today,
            today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.UpcomingTasks.Should().BeEmpty();
        response.CompletedTasks.Should().ContainSingle();
    }


    [Fact]
    public async Task GetToday_HabitStack_OnlyActiveStacks_ShouldAppear()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var activeStack = await DataBuilder.CreateHabitStackAsync(user.Id, "Active Stack", true);
        var inactiveStack = await DataBuilder.CreateHabitStackAsync(user.Id, "Inactive Stack", false);

        await DataBuilder.CreateHabitStackItemAsync(activeStack.Id, "wake up", "drink water");
        await DataBuilder.CreateHabitStackItemAsync(inactiveStack.Id, "sleep", "dream");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.HabitStacks.Should().ContainSingle(s => s.Name == "Active Stack");
        response.HabitStacks.Should().NotContain(s => s.Name == "Inactive Stack");
    }

    [Fact]
    public async Task GetToday_HabitStack_CompletionStatus_ReflectsTargetDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Morning Routine");
        var item = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "wake up", "drink water");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await DataBuilder.CreateHabitStackItemCompletionAsync(item.Id, today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        var stackResponse = response!.HabitStacks.Should().ContainSingle().Which;
        stackResponse.Items.Should().ContainSingle().Which.IsCompletedToday.Should().BeTrue();
        stackResponse.CompletedCount.Should().Be(1);
        stackResponse.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetToday_HabitStack_OrderedBySortOrder()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateHabitStackAsync(user.Id, "Third Stack", sortOrder: 2);
        await DataBuilder.CreateHabitStackAsync(user.Id, "First Stack", sortOrder: 0);
        await DataBuilder.CreateHabitStackAsync(user.Id, "Second Stack", sortOrder: 1);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.HabitStacks.Should().HaveCount(3);
        response.HabitStacks[0].Name.Should().Be("First Stack");
        response.HabitStacks[1].Name.Should().Be("Second Stack");
        response.HabitStacks[2].Name.Should().Be("Third Stack");
    }


    [Fact]
    public async Task GetToday_IdentityFeedback_OnlyShowsIdentitiesWithCompletions()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var identityWithCompletion = await DataBuilder.CreateIdentityAsync(user.Id, "Active Writer");
        var identityWithoutCompletion = await DataBuilder.CreateIdentityAsync(user.Id, "Lazy Reader");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await DataBuilder.CreateTaskAsync(
            goal.Id,
            "Writing task",
            TaskItemStatus.Completed,
            completedAt: today,
            identityId: identityWithCompletion.Id);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.IdentityFeedback.Should().ContainSingle(i => i.Name == "Active Writer");
        response.IdentityFeedback.Should().NotContain(i => i.Name == "Lazy Reader");
    }

    [Fact]
    public async Task GetToday_IdentityFeedback_CountsHabitAndTaskCompletions()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var identity = await DataBuilder.CreateIdentityAsync(user.Id, "Health Nut");
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Health Routine", identityId: identity.Id);
        var habitItem = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "wake up", "stretch");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create task completion
        await DataBuilder.CreateTaskAsync(
            goal.Id,
            "Health task",
            TaskItemStatus.Completed,
            completedAt: today,
            identityId: identity.Id);

        // Create habit completion
        await DataBuilder.CreateHabitStackItemCompletionAsync(habitItem.Id, today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        var feedback = response!.IdentityFeedback.Should().ContainSingle().Which;
        // 1 habit completion = 1 vote, 1 task completion = 2 votes, 1 fully completed stack = 2 bonus
        feedback.TotalVotes.Should().Be(5);
        feedback.HabitVotes.Should().Be(1);
        feedback.StackBonusVotes.Should().Be(2);
        feedback.TaskVotes.Should().Be(2);
    }


    [Fact]
    public async Task GetToday_ReturnsOnlyCurrentUserData()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();

        var goal1 = await DataBuilder.CreateGoalAsync(user1.Id);
        var goal2 = await DataBuilder.CreateGoalAsync(user2.Id);

        await DataBuilder.CreateTaskAsync(goal1.Id, "User 1 Task");
        await DataBuilder.CreateTaskAsync(goal2.Id, "User 2 Task");

        var stack1 = await DataBuilder.CreateHabitStackAsync(user1.Id, "User 1 Stack");
        var stack2 = await DataBuilder.CreateHabitStackAsync(user2.Id, "User 2 Stack");

        // Act
        Client.AuthenticateAs(user1.Id);
        var response = await Client.GetFromJsonAsync<TodayViewResponse>("/api/today");

        // Assert
        response.Should().NotBeNull();
        response!.UpcomingTasks.Should().ContainSingle(t => t.Title == "User 1 Task");
        response.UpcomingTasks.Should().NotContain(t => t.Title == "User 2 Task");
        response.HabitStacks.Should().ContainSingle(s => s.Name == "User 1 Stack");
        response.HabitStacks.Should().NotContain(s => s.Name == "User 2 Stack");
    }

    [Fact]
    public async Task GetToday_Unauthorized_Returns401()
    {
        // Arrange
        Client.ClearAuthentication();

        // Act
        var response = await Client.GetAsync("/api/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

// Response DTOs for deserialization
public record TodayViewResponse(
    DateOnly Date,
    List<TodayHabitStackResponse> HabitStacks,
    List<TodayTaskResponse> UpcomingTasks,
    List<TodayTaskResponse> CompletedTasks,
    List<TodayIdentityFeedbackResponse> IdentityFeedback
);

public record TodayHabitStackResponse(
    Guid Id,
    string Name,
    string? TriggerCue,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityColor,
    string? IdentityIcon,
    List<TodayHabitStackItemResponse> Items,
    int CompletedCount,
    int TotalCount
);

public record TodayHabitStackItemResponse(
    Guid Id,
    string HabitDescription,
    bool IsCompletedToday,
    int CurrentStreak
);

public record TodayTaskResponse(
    Guid Id,
    string Title,
    string? Description,
    Guid GoalId,
    string GoalTitle,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityIcon,
    string? IdentityColor,
    DateOnly? DueDate,
    string Status
);

public record TodayIdentityFeedbackResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int TotalVotes,
    int HabitVotes,
    int StackBonusVotes,
    int TaskVotes,
    int ProofVotes,
    string ReinforcementMessage
);