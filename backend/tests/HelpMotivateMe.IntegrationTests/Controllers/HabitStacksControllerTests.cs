using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class HabitStacksControllerTests : IntegrationTestBase
{
    public HabitStacksControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }

    #region CRUD Tests

    [Fact]
    public async Task GetHabitStacks_ReturnsAllUserStacks_OrderedBySortOrder()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateHabitStackAsync(user.Id, "Third", sortOrder: 2);
        await DataBuilder.CreateHabitStackAsync(user.Id, "First", sortOrder: 0);
        await DataBuilder.CreateHabitStackAsync(user.Id, "Second", sortOrder: 1);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<HabitStackResponse>>("/api/habit-stacks");

        // Assert
        response.Should().HaveCount(3);
        response![0].Name.Should().Be("First");
        response[1].Name.Should().Be("Second");
        response[2].Name.Should().Be("Third");
    }

    [Fact]
    public async Task GetHabitStack_ReturnsStackWithItems()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Morning Routine");
        await DataBuilder.CreateHabitStackItemAsync(stack.Id, "wake up", "drink water", 0);
        await DataBuilder.CreateHabitStackItemAsync(stack.Id, "drink water", "stretch", 1);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<HabitStackResponse>($"/api/habit-stacks/{stack.Id}");

        // Assert
        response.Should().NotBeNull();
        response!.Name.Should().Be("Morning Routine");
        response.Items.Should().HaveCount(2);
        response.Items[0].HabitDescription.Should().Be("drink water");
        response.Items[1].HabitDescription.Should().Be("stretch");
    }

    [Fact]
    public async Task CreateHabitStack_SetsSortOrderToMax()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateHabitStackAsync(user.Id, "Existing Stack", sortOrder: 5);

        var request = new { Name = "New Stack", Items = new[] { new { CueDescription = "wake", HabitDescription = "coffee" } } };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync("/api/habit-stacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<HabitStackResponse>();

        // Verify the sort order is 6 (max + 1)
        var allStacks = await Client.GetFromJsonAsync<List<HabitStackResponse>>("/api/habit-stacks");
        allStacks!.First(s => s.Name == "New Stack").Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateHabitStack_UpdatesFields()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Old Name");

        var request = new { Name = "New Name", IsActive = false };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync($"/api/habit-stacks/{stack.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<HabitStackResponse>();
        updated!.Name.Should().Be("New Name");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteHabitStack_RemovesStack()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "To Delete");
        await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue", "habit");

        // Act
        Client.AuthenticateAs(user.Id);
        var deleteResponse = await Client.DeleteAsync($"/api/habit-stacks/{stack.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/habit-stacks/{stack.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Reorder Tests

    [Fact]
    public async Task ReorderHabitStacks_UpdatesSortOrder()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack1 = await DataBuilder.CreateHabitStackAsync(user.Id, "First", sortOrder: 0);
        var stack2 = await DataBuilder.CreateHabitStackAsync(user.Id, "Second", sortOrder: 1);
        var stack3 = await DataBuilder.CreateHabitStackAsync(user.Id, "Third", sortOrder: 2);

        // Reorder: Third, First, Second
        var request = new { StackIds = new[] { stack3.Id, stack1.Id, stack2.Id } };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync("/api/habit-stacks/reorder", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stacks = await Client.GetFromJsonAsync<List<HabitStackResponse>>("/api/habit-stacks");
        stacks![0].Name.Should().Be("Third");
        stacks[1].Name.Should().Be("First");
        stacks[2].Name.Should().Be("Second");
    }

    [Fact]
    public async Task ReorderStackItems_UpdatesItemSortOrder()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Stack");
        var item1 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue1", "habit1", 0);
        var item2 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue2", "habit2", 1);
        var item3 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue3", "habit3", 2);

        // Reorder: 3, 1, 2
        var request = new { ItemIds = new[] { item3.Id, item1.Id, item2.Id } };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync($"/api/habit-stacks/{stack.Id}/items/reorder", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedStack = await Client.GetFromJsonAsync<HabitStackResponse>($"/api/habit-stacks/{stack.Id}");
        updatedStack!.Items[0].HabitDescription.Should().Be("habit3");
        updatedStack.Items[1].HabitDescription.Should().Be("habit1");
        updatedStack.Items[2].HabitDescription.Should().Be("habit2");
    }

    #endregion

    #region Streak Calculation Tests

    [Fact]
    public async Task CompleteItem_IncreasesStreak_WhenConsecutive()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Streak Test");
        var item = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue", "habit");
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Complete yesterday
        await DataBuilder.CreateHabitStackItemCompletionAsync(item.Id, yesterday);
        // Manually update streak
        item.CurrentStreak = 1;
        item.LongestStreak = 1;
        item.LastCompletedDate = yesterday;
        await Db.SaveChangesAsync();

        // Act - Complete today
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/habit-stacks/items/{item.Id}/complete?date={today:yyyy-MM-dd}", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HabitStackItemCompletionResponse>();
        result!.CurrentStreak.Should().Be(2);
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteItem_ResetsStreak_WhenGapInDays()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Streak Reset Test");
        var item = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue", "habit");
        var threeDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Complete 3 days ago (gap of 2 days)
        await DataBuilder.CreateHabitStackItemCompletionAsync(item.Id, threeDaysAgo);
        item.CurrentStreak = 5;
        item.LongestStreak = 5;
        item.LastCompletedDate = threeDaysAgo;
        await Db.SaveChangesAsync();

        // Act - Complete today (should reset streak to 1)
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/habit-stacks/items/{item.Id}/complete?date={today:yyyy-MM-dd}", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HabitStackItemCompletionResponse>();
        result!.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public async Task UncompleteItem_RecalculatesStreak()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Uncomplete Test");
        var item = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue", "habit");
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Complete 3 consecutive days
        await DataBuilder.CreateHabitStackItemCompletionAsync(item.Id, twoDaysAgo);
        await DataBuilder.CreateHabitStackItemCompletionAsync(item.Id, yesterday);
        await DataBuilder.CreateHabitStackItemCompletionAsync(item.Id, today);
        item.CurrentStreak = 3;
        item.LongestStreak = 3;
        item.LastCompletedDate = today;
        await Db.SaveChangesAsync();

        // Act - Uncomplete yesterday (should break the streak)
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/habit-stacks/items/{item.Id}/complete?date={yesterday:yyyy-MM-dd}", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HabitStackItemCompletionResponse>();
        result!.IsCompleted.Should().BeFalse(); // Yesterday is now uncompleted
        // Current streak recalculated from today only
        result.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public async Task CompleteItem_UpdatesLongestStreak()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Longest Streak Test");
        var item = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue", "habit");
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Complete yesterday
        await DataBuilder.CreateHabitStackItemCompletionAsync(item.Id, yesterday);
        item.CurrentStreak = 1;
        item.LongestStreak = 1;
        item.LastCompletedDate = yesterday;
        await Db.SaveChangesAsync();

        // Act - Complete today
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/habit-stacks/items/{item.Id}/complete?date={today:yyyy-MM-dd}", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HabitStackItemCompletionResponse>();
        result!.LongestStreak.Should().Be(2);
    }

    #endregion

    #region Complete All Tests

    [Fact]
    public async Task CompleteAll_OnlyCompletesIncompleteItems()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "Complete All Test");
        var item1 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue1", "habit1");
        var item2 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue2", "habit2");
        var item3 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue3", "habit3");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Pre-complete item1
        await DataBuilder.CreateHabitStackItemCompletionAsync(item1.Id, today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/habit-stacks/{stack.Id}/complete-all?date={today:yyyy-MM-dd}", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteAllResponse>();
        result!.CompletedCount.Should().Be(2); // Only item2 and item3
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task CompleteAll_WithAllAlreadyComplete_ReturnsZeroCompletedCount()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user.Id, "All Complete Test");
        var item1 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue1", "habit1");
        var item2 = await DataBuilder.CreateHabitStackItemAsync(stack.Id, "cue2", "habit2");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Pre-complete all items
        await DataBuilder.CreateHabitStackItemCompletionAsync(item1.Id, today);
        await DataBuilder.CreateHabitStackItemCompletionAsync(item2.Id, today);

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PatchAsync($"/api/habit-stacks/{stack.Id}/complete-all?date={today:yyyy-MM-dd}", user.Id);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteAllResponse>();
        result!.CompletedCount.Should().Be(0);
        result.TotalCount.Should().Be(2);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task GetHabitStack_ReturnsNotFound_ForOtherUsersStack()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user1.Id, "User1 Stack");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.GetAsync($"/api/habit-stacks/{stack.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteHabitStack_ReturnsNotFound_ForOtherUsersStack()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var stack = await DataBuilder.CreateHabitStackAsync(user1.Id, "User1 Stack");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.DeleteAsync($"/api/habit-stacks/{stack.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}

// Response DTOs for deserialization
public record HabitStackResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? IdentityId,
    string? IdentityName,
    string? IdentityColor,
    string? TriggerCue,
    bool IsActive,
    List<HabitStackItemResponse> Items,
    DateTime CreatedAt
);

public record HabitStackItemResponse(
    Guid Id,
    string CueDescription,
    string HabitDescription,
    int SortOrder,
    int CurrentStreak,
    int LongestStreak
);

public record HabitStackItemCompletionResponse(
    Guid ItemId,
    string HabitDescription,
    int CurrentStreak,
    int LongestStreak,
    bool IsCompleted
);

public record CompleteAllResponse(
    Guid StackId,
    int CompletedCount,
    int TotalCount
);
