using System.Net;
using System.Net.Http.Json;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class CategoriesControllerTests : IntegrationTestBase
{
    public CategoriesControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }

    #region CRUD Tests

    [Fact]
    public async Task GetCategories_ReturnsAllUserCategories_OrderedByName()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateCategoryAsync(user.Id, "Zebra");
        await DataBuilder.CreateCategoryAsync(user.Id, "Alpha");
        await DataBuilder.CreateCategoryAsync(user.Id, "Middle");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        // Assert
        response.Should().HaveCount(3);
        response![0].Name.Should().Be("Alpha");
        response[1].Name.Should().Be("Middle");
        response[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetCategory_ReturnsCategory()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user.Id, "Work", "#FF0000", "briefcase");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<CategoryDto>($"/api/categories/{category.Id}");

        // Assert
        response.Should().NotBeNull();
        response!.Name.Should().Be("Work");
        response.Color.Should().Be("#FF0000");
        response.Icon.Should().Be("briefcase");
    }

    [Fact]
    public async Task CreateCategory_CreatesNewCategory()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var request = new
        {
            Name = "Health",
            Color = "#00FF00",
            Icon = "heart"
        };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync("/api/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CategoryDto>();
        created!.Name.Should().Be("Health");
        created.Color.Should().Be("#00FF00");
        created.Icon.Should().Be("heart");
    }

    [Fact]
    public async Task CreateCategory_RejectsDuplicateName()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateCategoryAsync(user.Id, "Work");

        var request = new { Name = "Work" };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsJsonAsync("/api/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        error!.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateCategory_AllowsSameNameForDifferentUsers()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        await DataBuilder.CreateCategoryAsync(user1.Id, "Work");

        var request = new { Name = "Work" };

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.PostAsJsonAsync("/api/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateCategory_UpdatesFields()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user.Id, "Old Name", "#000000");

        var request = new
        {
            Name = "New Name",
            Color = "#FFFFFF",
            Icon = "star"
        };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        updated!.Name.Should().Be("New Name");
        updated.Color.Should().Be("#FFFFFF");
        updated.Icon.Should().Be("star");
    }

    [Fact]
    public async Task UpdateCategory_RejectsDuplicateName_ExcludingSelf()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category1 = await DataBuilder.CreateCategoryAsync(user.Id, "Work");
        var category2 = await DataBuilder.CreateCategoryAsync(user.Id, "Personal");

        // Try to rename category2 to "Work"
        var request = new { Name = "Work" };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync($"/api/categories/{category2.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_AllowsKeepingSameName()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user.Id, "Work", "#000000");

        // Update other fields but keep same name
        var request = new
        {
            Name = "Work",
            Color = "#FF0000"
        };

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        updated!.Color.Should().Be("#FF0000");
    }

    [Fact]
    public async Task DeleteCategory_RemovesCategory()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user.Id, "To Delete");

        // Act
        Client.AuthenticateAs(user.Id);
        var deleteResponse = await Client.DeleteAsync($"/api/categories/{category.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/categories/{category.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_DoesNotDeleteAssociatedGoals()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user.Id, "Work");
        var goal = await DataBuilder.CreateGoalAsync(user.Id, "Goal with category");
        goal.Categories.Add(category);
        await Db.SaveChangesAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var deleteResponse = await Client.DeleteAsync($"/api/categories/{category.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Goal should still exist
        var goalResponse = await Client.GetAsync($"/api/goals/{goal.Id}");
        goalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task GetCategory_ReturnsNotFound_ForOtherUsersCategory()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user1.Id, "User1 Category");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.GetAsync($"/api/categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_ReturnsNotFound_ForOtherUsersCategory()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user1.Id, "User1 Category");

        var request = new { Name = "Hijacked" };

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNotFound_ForOtherUsersCategory()
    {
        // Arrange
        var user1 = await DataBuilder.CreateUserAsync();
        var user2 = await DataBuilder.CreateUserAsync();
        var category = await DataBuilder.CreateCategoryAsync(user1.Id, "User1 Category");

        // Act
        Client.AuthenticateAs(user2.Id);
        var response = await Client.DeleteAsync($"/api/categories/{category.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}

// Response DTOs for deserialization
public record CategoryDto(
    Guid Id,
    string Name,
    string? Color,
    string? Icon
);

public record ErrorDto(string Message);
