using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TreeDrive.API.Controllers;
using TreeDrive.Infrastructure.Repositories;
using TreeDrive.Infrastructure.Data;
using TreeDrive.Shared.DTOs;
using TreeDrive.Core.Models;
using System.Text.Json;

namespace TreeDrive.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<UserRepository> _mockUserRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // Create a mock for MongoDbContext
        var mockConfig = new Mock<IConfiguration>();
        var mockDbContext = new Mock<MongoDbContext>(mockConfig.Object);
        _mockUserRepository = new Mock<UserRepository>(mockDbContext.Object);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        
        // Setup JWT config
        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("TestSecretKey12345678901234567890");
        _mockConfiguration.Setup(x => x.GetSection("Jwt:Secret")).Returns(configSection.Object);
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("TreeDrive");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("TreeDriveUsers");

        _controller = new AuthController(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockUserRepository.Object
        );
    }

    [Fact]
    public async Task Register_WithValidUser_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "password123"
        };
        
        _mockUserRepository
            .Setup(x => x.UserExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        
        response.Should().NotBeNull();
        response!.Should().ContainKey("message");
        response["message"].Should().Contain("Registration successful");
    }

    [Fact]
    public async Task Register_WithExistingUser_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Password = "password123"
        };
        
        _mockUserRepository
            .Setup(x => x.UserExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };
        
        var user = new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _mockUserRepository
            .Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.UpdateLastLoginAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        
        var response = okResult.Value as LoginResponse;
        response.Should().NotBeNull();
        if (response != null)
        {
            response.Token.Should().NotBeNullOrEmpty();
            response.Success.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };
        
        var user = new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _mockUserRepository
            .Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "password123"
        };
        
        _mockUserRepository
            .Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}