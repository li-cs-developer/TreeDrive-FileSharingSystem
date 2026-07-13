using Xunit;
using FluentAssertions;

namespace TreeDrive.Integration.Tests;

public class BasicTest
{
    [Fact]
    public void Test_True_ShouldPass()
    {
        // Arrange & Act & Assert
        true.Should().BeTrue();
    }
}