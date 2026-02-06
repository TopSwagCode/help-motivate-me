using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.IntegrationTests.Helpers;
using HelpMotivateMe.IntegrationTests.Infrastructure;

namespace HelpMotivateMe.IntegrationTests.Controllers;

[Collection("Database")]
public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(DatabaseFixture dbFixture) : base(dbFixture)
    {
    }


    [Fact]
    public async Task Register_CreatesNewUser()
    {
        // Arrange
        var request = new
        {
            Email = "newuser@example.com",
            Password = "SecureP@ss123",
            DisplayName = "New User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result!.Email.Should().Be("newuser@example.com");
        result.Message.Should().Contain("verify");
    }

    [Fact]
    public async Task Register_RejectsExistingEmail()
    {
        // Arrange
        var existingUser = await DataBuilder.CreateUserAsync();

        var request = new
        {
            Email = existingUser.Email,
            Password = "SecureP@ss123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Message.Should().Contain("Email already exists");
    }



    [Fact]
    public async Task Login_WorksWithEmail()
    {
        // Arrange
        var password = "TestPassword123!";
        var user = await CreateUserWithPasswordAsync("test@example.com", password);

        var request = new
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loggedIn = await response.Content.ReadFromJsonAsync<UserResponse>();
        loggedIn!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_RejectsInvalidPassword()
    {
        // Arrange
        var user = await CreateUserWithPasswordAsync("test@example.com", "CorrectPassword");

        var request = new
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_RejectsNonExistentUser()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_RejectsDisabledUser()
    {
        // Arrange
        var password = "TestPassword123!";
        var user = await CreateUserWithPasswordAsync("disabled@example.com", password);
        user.IsActive = false;
        await Db.SaveChangesAsync();

        var request = new
        {
            Email = "disabled@example.com",
            Password = password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Message.Should().Contain("disabled");
    }

    [Fact]
    public async Task Login_RejectsUserWithOnlyExternalLogin()
    {
        // Arrange - Create user without password (OAuth only)
        var user = await DataBuilder.CreateUserAsync();
        user.PasswordHash = null;
        await Db.SaveChangesAsync();

        var request = new
        {
            Email = user.Email,
            Password = "AnyPassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    [Fact]
    public async Task GetMe_ReturnsCurrentUser()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.GetFromJsonAsync<UserResponse>("/api/auth/me");

        // Assert
        response.Should().NotBeNull();
        response!.Id.Should().Be(user.Id);
        response.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetMe_ReturnsUnauthorized_WhenNotLoggedIn()
    {
        // Act - No authentication
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    [Fact]
    public async Task Unlink_RemovesExternalLogin()
    {
        // Arrange
        var user = await CreateUserWithPasswordAsync("link@example.com", "Password123!");
        var externalLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "github",
            ProviderKey = "12345",
            ProviderDisplayName = "github_user"
        };
        Db.UserExternalLogins.Add(externalLogin);
        await Db.SaveChangesAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.DeleteAsync("/api/auth/unlink/github");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unlink_ReturnsNotFound_ForNonExistentProvider()
    {
        // Arrange
        var user = await CreateUserWithPasswordAsync("link@example.com", "Password123!");

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.DeleteAsync("/api/auth/unlink/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unlink_PreventsRemovingLastLoginMethod()
    {
        // Arrange - User with only external login (no password)
        var user = await DataBuilder.CreateUserAsync();
        user.PasswordHash = null;
        var externalLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "github",
            ProviderKey = "12345",
            ProviderDisplayName = "github_user"
        };
        Db.UserExternalLogins.Add(externalLogin);
        await Db.SaveChangesAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.DeleteAsync("/api/auth/unlink/github");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Message.Should().Contain("Cannot remove last login method");
    }

    [Fact]
    public async Task Unlink_AllowsRemovingExternalLogin_WhenPasswordExists()
    {
        // Arrange - User with password AND external login
        var user = await CreateUserWithPasswordAsync("multi@example.com", "Password123!");
        var externalLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "github",
            ProviderKey = "12345",
            ProviderDisplayName = "github_user"
        };
        Db.UserExternalLogins.Add(externalLogin);
        await Db.SaveChangesAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.DeleteAsync("/api/auth/unlink/github");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unlink_AllowsRemovingExternalLogin_WhenOtherExternalLoginsExist()
    {
        // Arrange - User with two external logins, no password
        var user = await DataBuilder.CreateUserAsync();
        user.PasswordHash = null;

        var githubLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "github",
            ProviderKey = "github_123",
            ProviderDisplayName = "github_user"
        };
        var googleLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "google",
            ProviderKey = "google_456",
            ProviderDisplayName = "google_user"
        };
        Db.UserExternalLogins.AddRange(githubLogin, googleLogin);
        await Db.SaveChangesAsync();

        // Act - Remove github, but google should remain
        Client.AuthenticateAs(user.Id);
        var response = await Client.DeleteAsync("/api/auth/unlink/github");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }



    [Fact]
    public async Task Logout_ReturnsNoContent()
    {
        // Arrange
        var user = await DataBuilder.CreateUserAsync();

        // Act
        Client.AuthenticateAs(user.Id);
        var response = await Client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_ReturnsUnauthorized_WhenNotLoggedIn()
    {
        // Act - No authentication
        var response = await Client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    private async Task<User> CreateUserWithPasswordAsync(string email, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = HashPassword(password),
            DisplayName = email.Split('@')[0],
            IsActive = true,
            IsEmailVerified = true, // Verified for testing
            CreatedAt = DateTime.UtcNow
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

}

// Response DTOs for deserialization
public record UserResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    DateTime CreatedAt,
    IEnumerable<string> ExternalLogins
);

public record RegisterResponse(string Message, string Email);

public record ErrorResponse(string Message);
