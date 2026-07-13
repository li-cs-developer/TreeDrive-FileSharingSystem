using Xunit;
using FluentAssertions;
using TreeDrive.Infrastructure.Helpers;

namespace TreeDrive.Infrastructure.Tests.Helpers;

public class PasswordHelperTests
{
    [Fact]
    public void HashPassword_ShouldReturnHashedString()
    {
        // Arrange
        var password = "myPassword123";

        // Act
        var hash = PasswordHelper.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "myPassword123";
        var hash = PasswordHelper.HashPassword(password);

        // Act
        var result = PasswordHelper.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "myPassword123";
        var wrongPassword = "wrongPassword";
        var hash = PasswordHelper.HashPassword(password);

        // Act
        var result = PasswordHelper.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ShouldBeDifferentForSamePassword()
    {
        // Arrange
        var password = "myPassword123";

        // Act
        var hash1 = PasswordHelper.HashPassword(password);
        var hash2 = PasswordHelper.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses random salt
    }
}