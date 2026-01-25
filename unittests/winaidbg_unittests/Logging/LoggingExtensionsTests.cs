using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Logging;

using Xunit;

namespace WinAiDbg.Tests.Logging;

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
    /// Verifies that AddWinAiDbgLogging returns the logging builder for chaining.
    /// </summary>
    [Fact]
    public void AddWinAiDbgLogging_ReturnsLoggingBuilder()
    {
        // Arrange
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();

        // Act
        var result = mockLoggingBuilder.Object.AddWinAiDbgLogging(m_Settings.Object, false);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeSameAs(mockLoggingBuilder.Object);
    }

    /// <summary>
    /// Verifies that AddWinAiDbgLogging can be called with service mode enabled.
    /// </summary>
    [Fact]
    public void AddWinAiDbgLogging_WithServiceMode_ReturnsLoggingBuilder()
    {
        // Arrange
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();

        // Act
        var result = mockLoggingBuilder.Object.AddWinAiDbgLogging(m_Settings.Object, true);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeSameAs(mockLoggingBuilder.Object);
    }
}
