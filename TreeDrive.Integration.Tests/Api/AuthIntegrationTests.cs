using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
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
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        result.Should().NotBeNull();
        result!["message"].Should().Contain("Registration successful");
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
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - Then login
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var username = $"wrongpass_test_{Guid.NewGuid():N}";
        var password = "password123";
        
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = "wrongpassword"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}