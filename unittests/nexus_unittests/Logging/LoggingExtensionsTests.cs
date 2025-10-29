using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Nexus.Logging;

using Xunit;

namespace Nexus.Tests.Logging;

/// <summary>
/// Unit tests for the LoggingExtensions class.
/// </summary>
public class LoggingExtensionsTests
{
    /// <summary>
    /// Verifies that AddNexusLogging returns the logging builder for chaining.
    /// </summary>
    [Fact]
    public void AddNexusLogging_ReturnsLoggingBuilder()
    {
        // Arrange
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();

        // Act
        var result = mockLoggingBuilder.Object.AddNexusLogging(false);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeSameAs(mockLoggingBuilder.Object);
    }

    /// <summary>
    /// Verifies that AddNexusLogging can be called with service mode enabled.
    /// </summary>
    [Fact]
    public void AddNexusLogging_WithServiceMode_ReturnsLoggingBuilder()
    {
        // Arrange
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();

        // Act
        var result = mockLoggingBuilder.Object.AddNexusLogging(true);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeSameAs(mockLoggingBuilder.Object);
    }
}
