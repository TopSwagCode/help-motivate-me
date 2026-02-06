using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class TasksControllerTests : IntegrationTestBase
{
    public TasksControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }


    [Fact]
    public async Task GetTasks_ReturnsTopLevelTasksOnly()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var parentTask = await DataBuilder.CreateTaskAsync(goal.Id, "Parent Task");

        // Create subtask by directly setting ParentTaskId in DB
        var subtask = await DataBuilder.CreateTaskAsync(goal.Id, "Subtask");
        subtask.ParentTaskId = parentTask.Id;
        await Db.SaveChangesAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<TaskResponse>>($"/api/goals/{goal.Id}/tasks");

        // Assert
        response.Should().ContainSingle(t => t.Title == "Parent Task");
        response.Should().NotContain(t => t.Title == "Subtask");
    }

    [Fact]
    public async Task CreateTask_SetsSortOrderToMax()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        await DataBuilder.CreateTaskAsync(goal.Id, "Existing Task", sortOrder: 5);

        var request = new { Title = "New Task" };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync($"/api/goals/{goal.Id}/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();
        created!.SortOrder.Should().Be(6);
    }

    [Fact]
    public async Task CreateSubtask_LinksToParentTask()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var parentTask = await DataBuilder.CreateTaskAsync(goal.Id, "Parent Task");

        var request = new { Title = "Subtask" };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync($"/api/tasks/{parentTask.Id}/subtasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();
        created!.ParentTaskId.Should().Be(parentTask.Id);
        created.GoalId.Should().Be(goal.Id);
    }

    [Fact]
    public async Task DeleteTask_RemovesTask()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "To Delete");

        // Act
        Client.AuthenticateAs(user.Id);
        var deleteResponse = await Client.DeleteAsync($"/api/tasks/{task.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/tasks/{task.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    // [Fact] - REMOVED: Repeatable task feature has been removed
    // public async Task CompleteRepeatableTask_Daily_CalculatesNextOccurrence()

    // [Fact] - REMOVED: Repeatable task feature has been removed  
    // public async Task CompleteRepeatableTask_Weekly_CalculatesNextOccurrence()

    // [Fact] - REMOVED: Repeatable task feature has been removed
    // public async Task CompleteRepeatableTask_Monthly_CalculatesNextOccurrence()

    [Fact]
    public async Task CompleteNonRepeatableTask_MarksAsCompleted()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "One-time task");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/tasks/{task.Id}/complete", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<TaskResponse>();

        completed!.Status.Should().Be("Completed");
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteTask_Toggle_UncompletesIfAlreadyComplete()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "Toggle Task", TaskItemStatus.Completed,
            completedAt: today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/tasks/{task.Id}/complete", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await response.Content.ReadFromJsonAsync<TaskResponse>();

        toggled!.Status.Should().Be("Pending");
        toggled.CompletedAt.Should().BeNull();
    }


    [Fact]
    public async Task CreateTinyVersion_CreatesMicroTask()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var fullTask = await DataBuilder.CreateTaskAsync(goal.Id, "Write 1000 words");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsync($"/api/tasks/{fullTask.Id}/tiny-version", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var tinyTask = await response.Content.ReadFromJsonAsync<TaskResponse>();

        tinyTask!.Title.Should().StartWith("[2 min]");
        // The actual API response doesn't include these fields in the DTO,
        // but we can verify the task was created with the correct title
    }

    [Fact]
    public async Task CreateTinyVersion_PreventsDuplicates()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var fullTask = await DataBuilder.CreateTaskAsync(goal.Id, "Write 1000 words");

        // Create first tiny version
        Client.AuthenticateAs(user.Id);
        await Client.PostAsync($"/api/tasks/{fullTask.Id}/tiny-version", null);

        // Act - Try to create second tiny version
        var response = await Client.PostAsync($"/api/tasks/{fullTask.Id}/tiny-version", null);

        // Assert - API returns BadRequest (400), not Conflict (409)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task CompleteMultiple_CompletesAllSpecifiedTasks()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var task1 = await DataBuilder.CreateTaskAsync(goal.Id, "Task 1");
        var task2 = await DataBuilder.CreateTaskAsync(goal.Id, "Task 2");
        var task3 = await DataBuilder.CreateTaskAsync(goal.Id, "Task 3");

        var request = new { TaskIds = new[] { task1.Id, task2.Id } };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync("/api/tasks/complete-multiple", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteMultipleTasksResponse>();
        result!.CompletedCount.Should().Be(2);
        result.TotalCount.Should().Be(2);

        // Verify task3 is still pending
        var task3Response = await Client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{task3.Id}");
        task3Response!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CompleteMultiple_IgnoresOtherUsersTaskIds()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();

        var goal1 = await DataBuilder.CreateGoalAsync(user1.Id);
        var goal2 = await DataBuilder.CreateGoalAsync(user2.Id);

        var task1 = await DataBuilder.CreateTaskAsync(goal1.Id, "User1 Task");
        var task2 = await DataBuilder.CreateTaskAsync(goal2.Id, "User2 Task");

        var request = new { TaskIds = new[] { task1.Id, task2.Id } };

        // Act
        Client.AuthenticateAs(user1.Id);
        var response = await Client.PostAsJsonAsync("/api/tasks/complete-multiple", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteMultipleTasksResponse>();
        result!.CompletedCount.Should().Be(1); // Only user1's task
    }


    [Fact]
    public async Task ReorderTasks_UpdatesSortOrder()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var task1 = await DataBuilder.CreateTaskAsync(goal.Id, "First", sortOrder: 0);
        var task2 = await DataBuilder.CreateTaskAsync(goal.Id, "Second", sortOrder: 1);
        var task3 = await DataBuilder.CreateTaskAsync(goal.Id, "Third", sortOrder: 2);

        // Controller expects List<Guid> directly, not wrapped object
        var taskIds = new[] { task3.Id, task1.Id, task2.Id };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync("/api/tasks/reorder", taskIds);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var tasks = await Client.GetFromJsonAsync<List<TaskResponse>>($"/api/goals/{goal.Id}/tasks");
        tasks![0].Title.Should().Be("Third");
        tasks[1].Title.Should().Be("First");
        tasks[2].Title.Should().Be("Second");
    }


    [Fact]
    public async Task PostponeTask_UpdatesDueDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "Task to postpone", dueDate: today);

        var newDueDate = today.AddDays(7);
        var request = new { NewDueDate = newDueDate }; // API expects NewDueDate, not DueDate

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsJsonAsync($"/api/tasks/{task.Id}/postpone", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>();
        updated!.DueDate.Should().Be(newDueDate);
    }


    [Fact]
    public async Task GetTask_ReturnsNotFound_ForOtherUsersTask()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();

        var goal = await DataBuilder.CreateGoalAsync(user1.Id);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "User1 Task");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.GetAsync($"/api/tasks/{task.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTask_ReturnsNotFound_ForOtherUsersTask()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();

        var goal = await DataBuilder.CreateGoalAsync(user1.Id);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "User1 Task");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.DeleteAsync($"/api/tasks/{task.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task CompleteTask_WithClientDate_UsesProvidedDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "Task with client date");

        // Use a specific date that's different from today UTC
        var clientDate = new DateOnly(2025, 6, 15);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/tasks/{task.Id}/complete?date={clientDate:yyyy-MM-dd}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<TaskResponse>();

        completed!.Status.Should().Be("Completed");
        completed.CompletedAt.Should().Be(clientDate);
    }

    [Fact]
    public async Task CompleteTask_WithoutClientDate_UsesUtcDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "Task without client date");
        var expectedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/tasks/{task.Id}/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<TaskResponse>();

        completed!.Status.Should().Be("Completed");
        completed.CompletedAt.Should().Be(expectedDate);
    }

    [Fact]
    public async Task CompleteMultipleTasks_WithClientDate_UsesProvidedDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id);
        var task1 = await DataBuilder.CreateTaskAsync(goal.Id, "Task 1");
        var task2 = await DataBuilder.CreateTaskAsync(goal.Id, "Task 2");

        // Use a specific date that's different from today UTC
        var clientDate = new DateOnly(2025, 6, 15);
        var request = new { TaskIds = new[] { task1.Id, task2.Id }, Date = clientDate };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync("/api/tasks/complete-multiple", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteMultipleTasksResponse>();
        result!.CompletedCount.Should().Be(2);

        // Verify tasks have the client date
        var task1Response = await Client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{task1.Id}");
        task1Response!.CompletedAt.Should().Be(clientDate);

        var task2Response = await Client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{task2.Id}");
        task2Response!.CompletedAt.Should().Be(clientDate);
    }

    // [Fact] - REMOVED: Repeatable task feature has been removed
    // public async Task CompleteRepeatableTask_WithClientDate_CalculatesNextFromProvidedDate()
}

// Response DTOs for deserialization - matching the actual API response structure
public record TaskResponse(
    Guid Id,
    Guid GoalId,
    Guid? ParentTaskId,
    string Title,
    string? Description,
    string Status,
    DateOnly? DueDate,
    DateOnly? CompletedAt,
    int SortOrder,
    IEnumerable<TaskResponse>? Subtasks,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityIcon,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// REMOVED: RepeatScheduleResponse - repeatable task feature has been removed
// public record RepeatScheduleResponse(...)

public record CompleteMultipleTasksResponse(
    int CompletedCount,
    int TotalCount
);