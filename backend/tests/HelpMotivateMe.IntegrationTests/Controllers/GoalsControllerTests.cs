using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class GoalsControllerTests : IntegrationTestBase
{
    public GoalsControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }

    #region CRUD Tests

    [Fact]
    public async Task GetGoals_ReturnsAllUserGoals_OrderedBySortOrder()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateGoalAsync(user.Id, "Third", sortOrder: 2);
        await DataBuilder.CreateGoalAsync(user.Id, "First", sortOrder: 0);
        await DataBuilder.CreateGoalAsync(user.Id, "Second", sortOrder: 1);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<GoalResponse>>("/api/goals");

        // Assert
        response.Should().HaveCount(3);
        response![0].Title.Should().Be("First");
        response[1].Title.Should().Be("Second");
        response[2].Title.Should().Be("Third");
    }

    [Fact]
    public async Task GetGoals_FilterByCategory_ReturnsOnlyMatchingGoals()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user.Id, "Work");
        var goalWithCategory = await DataBuilder.CreateGoalAsync(user.Id, "Work Goal");
        var goalWithoutCategory = await DataBuilder.CreateGoalAsync(user.Id, "Personal Goal");

        // Associate category with goal
        goalWithCategory.Categories.Add(category);
        await Db.SaveChangesAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<GoalResponse>>($"/api/goals?categoryId={category.Id}");

        // Assert
        response.Should().ContainSingle();
        response![0].Title.Should().Be("Work Goal");
    }

    [Fact]
    public async Task GetGoal_ReturnsGoalWithTaskCounts()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Test Goal");
        await DataBuilder.CreateTaskAsync(goal.Id, "Completed Task", status: Core.Enums.TaskItemStatus.Completed);
        await DataBuilder.CreateTaskAsync(goal.Id, "Pending Task 1");
        await DataBuilder.CreateTaskAsync(goal.Id, "Pending Task 2");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<GoalResponse>($"/api/goals/{goal.Id}");

        // Assert
        response.Should().NotBeNull();
        response!.Title.Should().Be("Test Goal");
        response.TaskCount.Should().Be(3);
        response.CompletedTaskCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateGoal_SetsSortOrderToMax()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateGoalAsync(user.Id, "Existing Goal", sortOrder: 5);

        var request = new { Title = "New Goal" };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<GoalResponse>();
        created!.SortOrder.Should().Be(6);
    }

    [Fact]
    public async Task CreateGoal_WithCategories_AssociatesCategories()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category1 = await DataBuilder.CreateCategoryAsync(user.Id, "Work");
        var category2 = await DataBuilder.CreateCategoryAsync(user.Id, "Health");

        var request = new
        {
            Title = "Categorized Goal",
            CategoryIds = new[] { category1.Id, category2.Id }
        };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<GoalResponse>();
        created!.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateGoal_UpdatesFields()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Old Title");
        var newTargetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));

        var request = new
        {
            Title = "New Title",
            Description = "New Description",
            TargetDate = newTargetDate
        };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync($"/api/goals/{goal.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<GoalResponse>();
        updated!.Title.Should().Be("New Title");
        updated.Description.Should().Be("New Description");
        updated.TargetDate.Should().Be(newTargetDate);
    }

    [Fact]
    public async Task UpdateGoal_WithCategories_ReplacesCategories()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var oldCategory = await DataBuilder.CreateCategoryAsync(user.Id, "Old");
        var newCategory = await DataBuilder.CreateCategoryAsync(user.Id, "New");
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Goal");
        goal.Categories.Add(oldCategory);
        await Db.SaveChangesAsync();

        var request = new
        {
            Title = "Goal",
            CategoryIds = new[] { newCategory.Id }
        };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync($"/api/goals/{goal.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<GoalResponse>();
        updated!.Categories.Should().ContainSingle(c => c.Name == "New");
        updated.Categories.Should().NotContain(c => c.Name == "Old");
    }

    [Fact]
    public async Task DeleteGoal_RemovesGoal()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "To Delete");

        // Act
        Client.AuthenticateAs(user.Id);
        var deleteResponse = await Client.DeleteAsync($"/api/goals/{goal.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/goals/{goal.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteGoal_CascadesDeleteToTasks()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Goal with Tasks");
        var task = await DataBuilder.CreateTaskAsync(goal.Id, "Task to be deleted");

        // Act
        Client.AuthenticateAs(user.Id);
        var deleteResponse = await Client.DeleteAsync($"/api/goals/{goal.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify task is also deleted
        var taskResponse = await Client.GetAsync($"/api/tasks/{task.Id}");
        taskResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Complete Toggle Tests

    [Fact]
    public async Task CompleteGoal_TogglesIsCompleted_FromFalseToTrue()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Goal to Complete");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/goals/{goal.Id}/complete", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<GoalResponse>();
        completed!.IsCompleted.Should().BeTrue();
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteGoal_TogglesIsCompleted_FromTrueToFalse()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Completed Goal", isCompleted: true);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/goals/{goal.Id}/complete", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var uncompleted = await response.Content.ReadFromJsonAsync<GoalResponse>();
        uncompleted!.IsCompleted.Should().BeFalse();
        uncompleted.CompletedAt.Should().BeNull();
    }

    #endregion

    #region Reorder Tests

    [Fact]
    public async Task ReorderGoals_UpdatesSortOrder()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal1 = await DataBuilder.CreateGoalAsync(user.Id, "First", sortOrder: 0);
        var goal2 = await DataBuilder.CreateGoalAsync(user.Id, "Second", sortOrder: 1);
        var goal3 = await DataBuilder.CreateGoalAsync(user.Id, "Third", sortOrder: 2);

        // Reorder: Third, First, Second
        var reorderRequest = new[] { goal3.Id, goal1.Id, goal2.Id };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync("/api/goals/reorder", reorderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var goals = await Client.GetFromJsonAsync<List<GoalResponse>>("/api/goals");
        goals![0].Title.Should().Be("Third");
        goals[1].Title.Should().Be("First");
        goals[2].Title.Should().Be("Second");
    }

    [Fact]
    public async Task ReorderGoals_IgnoresOtherUsersGoalIds()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();

        var goal1 = await DataBuilder.CreateGoalAsync(user1.Id, "User1 Goal", sortOrder: 0);
        var goal2 = await DataBuilder.CreateGoalAsync(user2.Id, "User2 Goal", sortOrder: 0);

        // Try to include other user's goal in reorder
        var reorderRequest = new[] { goal2.Id, goal1.Id };

        // Act
        Client.AuthenticateAs(user1.Id);
        var response = await Client.PutAsJsonAsync("/api/goals/reorder", reorderRequest);

        // Assert - should succeed but only affect user1's goals
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // User2's goal should still have sortOrder 0
        Client.AuthenticateAs(user2.Id);
        var user2Goals = await Client.GetFromJsonAsync<List<GoalResponse>>("/api/goals");
        user2Goals![0].SortOrder.Should().Be(0);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task GetGoal_ReturnsNotFound_ForOtherUsersGoal()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user1.Id, "User1 Goal");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.GetAsync($"/api/goals/{goal.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateGoal_ReturnsNotFound_ForOtherUsersGoal()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user1.Id, "User1 Goal");

        var request = new { Title = "Hijacked" };

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.PutAsJsonAsync($"/api/goals/{goal.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteGoal_ReturnsNotFound_ForOtherUsersGoal()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user1.Id, "User1 Goal");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.DeleteAsync($"/api/goals/{goal.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteGoal_ReturnsNotFound_ForOtherUsersGoal()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user1.Id, "User1 Goal");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.PatchAsync($"/api/goals/{goal.Id}/complete", user2.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateGoal_CannotAssociateOtherUsersCategories()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var user1Category = await DataBuilder.CreateCategoryAsync(user1.Id, "User1 Category");

        var request = new
        {
            Title = "Goal with stolen category",
            CategoryIds = new[] { user1Category.Id }
        };

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.PostAsJsonAsync("/api/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<GoalResponse>();
        // Category should not be associated (filtered out)
        created!.Categories.Should().BeEmpty();
    }

    #endregion

    #region Client Date Parameter Tests

    [Fact]
    public async Task CompleteGoal_WithClientDate_UsesProvidedDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Goal with client date");

        // Use a specific date that's different from today UTC
        var clientDate = new DateOnly(2025, 6, 15);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/goals/{goal.Id}/complete?date={clientDate:yyyy-MM-dd}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<GoalResponse>();

        completed!.IsCompleted.Should().BeTrue();
        // CompletedAt should use the client-provided date
        completed.CompletedAt.Should().NotBeNull();
        var completedDate = DateOnly.FromDateTime(completed.CompletedAt!.Value);
        completedDate.Should().Be(clientDate);
    }

    [Fact]
    public async Task CompleteGoal_WithoutClientDate_UsesUtcDate()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Goal without client date");
        var expectedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/goals/{goal.Id}/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<GoalResponse>();

        completed!.IsCompleted.Should().BeTrue();
        completed.CompletedAt.Should().NotBeNull();
        var completedDate = DateOnly.FromDateTime(completed.CompletedAt!.Value);
        completedDate.Should().Be(expectedDate);
    }

    #endregion
}

// Response DTOs for deserialization
public record GoalResponse(
    Guid Id,
    string Title,
    string? Description,
    DateOnly? TargetDate,
    bool IsCompleted,
    DateTime? CompletedAt,
    int SortOrder,
    int TaskCount,
    int CompletedTaskCount,
    IEnumerable<CategoryResponse> Categories,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon
);
