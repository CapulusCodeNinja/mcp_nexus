using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Nexus.Config;
using Nexus.Logging;

using Xunit;

namespace Nexus.Tests.Logging;

/// <summary>
/// Unit tests for the LoggingExtensions class.
/// </summary>
public class LoggingExtensionsTests
{
    private readonly Mock<ISettings> m_Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingExtensionsTests"/> class.
    /// </summary>
    public LoggingExtensionsTests()
    {
        m_Settings = new Mock<ISettings>();
    }

    /// <summary>
    /// Verifies that AddNexusLogging returns the logging builder for chaining.
    /// </summary>
    [Fact]
    public void AddNexusLogging_ReturnsLoggingBuilder()
    {
        // Arrange
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();

        // Act
        var result = mockLoggingBuilder.Object.AddNexusLogging(m_Settings.Object, false);

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
        var result = mockLoggingBuilder.Object.AddNexusLogging(m_Settings.Object, true);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeSameAs(mockLoggingBuilder.Object);
    }
}
