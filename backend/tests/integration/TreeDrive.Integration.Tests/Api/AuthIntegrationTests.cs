using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TreeDrive.Shared.DTOs;

namespace TreeDrive.Integration.Tests.Api;

public class AuthIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(TestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewUser_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = $"integration_user_{Guid.NewGuid():N}",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        root.TryGetProperty("message", out var messageElement).Should().BeTrue();
        messageElement.GetString().Should().Contain("Registration successful");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange - First register a user
        var username = $"login_test_{Guid.NewGuid():N}";
        var password = "password123";
        
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Password = password
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Then login
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        root.TryGetProperty("success", out var successElement).Should().BeTrue();
        successElement.GetBoolean().Should().BeTrue();
        
        root.TryGetProperty("token", out var tokenElement).Should().BeTrue();
        tokenElement.GetString().Should().NotBeNullOrEmpty();
        
        root.TryGetProperty("username", out var usernameElement).Should().BeTrue();
        usernameElement.GetString().Should().Be(username);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsErrorMessage()
    {
        // Arrange
        var username = $"wrongpass_test_{Guid.NewGuid():N}";
        var password = "password123";
        
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Password = password
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Login with wrong password
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = "wrongpassword"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert - Check that response contains error message (regardless of status code)
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        // Either status is 401 with error, or 200 with success:false
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Invalid username or password");
        }
        else
        {
            // If it returns 200, check for success:false
            root.TryGetProperty("success", out var successElement).Should().BeTrue();
            successElement.GetBoolean().Should().BeFalse();
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Invalid username or password");
        }
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsErrorMessage()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "nonexistent_user_12345",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert - Check that response contains error message
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Invalid username or password");
        }
        else
        {
            root.TryGetProperty("success", out var successElement).Should().BeTrue();
            successElement.GetBoolean().Should().BeFalse();
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Invalid username or password");
        }
    }

    [Fact]
    public async Task Register_ExistingUser_ReturnsErrorMessage()
    {
        // Arrange - First register a user
        var username = $"existing_test_{Guid.NewGuid():N}";
        var password = "password123";
        
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - Try to register with same username
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert - Check for error message
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Username already exists");
        }
        else
        {
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Username already exists");
        }
    }

    [Fact]
    public async Task Register_InvalidPassword_ReturnsErrorMessage()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = $"invalid_pass_{Guid.NewGuid():N}",
            Password = "123" // Too short
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - Check for error message
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Password must be at least 6 characters");
        }
        else
        {
            root.TryGetProperty("message", out var messageElement).Should().BeTrue();
            messageElement.GetString().Should().Contain("Password must be at least 6 characters");
        }
    }
}