using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

using MELogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Nexus.Config_unittests.Internal;

/// <summary>
/// Unit tests for LoggingConfiguration.
/// </summary>
public class LoggingConfigurationTests
{
    private readonly Mock<IConfiguration> m_MockConfiguration;
    private readonly Mock<ILoggingBuilder> m_MockLoggingBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingConfigurationTests"/> class.
    /// </summary>
    public LoggingConfigurationTests()
    {
        m_MockConfiguration = new Mock<IConfiguration>();
        m_MockLoggingBuilder = new Mock<ILoggingBuilder>();

        // Setup default configuration
        _ = m_MockConfiguration.Setup(x => x["Logging:LogLevel"]).Returns("Information");
    }

    /// <summary>
    /// Tests that ParseLogLevel handles various log level strings correctly.
    /// </summary>
    [Theory]
    [InlineData("trace", MELogLevel.Trace)]
    [InlineData("debug", MELogLevel.Debug)]
    [InlineData("information", MELogLevel.Information)]
    [InlineData("info", MELogLevel.Information)]
    [InlineData("warning", MELogLevel.Warning)]
    [InlineData("warn", MELogLevel.Warning)]
    [InlineData("error", MELogLevel.Error)]
    [InlineData("critical", MELogLevel.Critical)]
    [InlineData("none", MELogLevel.None)]
    [InlineData("invalid", MELogLevel.Information)]
    [InlineData("", MELogLevel.Information)]
    [InlineData(null, MELogLevel.Information)]
    public void ParseLogLevel_WithVariousInputs_ShouldReturnCorrectLevel(string? logLevelString, MELogLevel expectedLevel)
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();

        // Act
        var result = testAccessor.TestParseLogLevel(logLevelString!);

        // Assert
        _ = result.Should().Be(expectedLevel);
    }

    /// <summary>
    /// Tests that GetNLogLevel converts Microsoft LogLevel to NLog LogLevel correctly.
    /// </summary>
    [Theory]
    [InlineData("Trace", "Trace")]
    [InlineData("Debug", "Debug")]
    [InlineData("Information", "Info")]
    [InlineData("Warning", "Warn")]
    [InlineData("Error", "Error")]
    [InlineData("Critical", "Fatal")]
    [InlineData("None", "Off")]
    public void GetNLogLevel_WithMicrosoftLogLevel_ShouldReturnCorrectNLogLevel(string microsoftLevelName, string expectedNLogLevelName)
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();
        var microsoftLevel = Enum.Parse<MELogLevel>(microsoftLevelName);
        var expectedNLogLevel = NLog.LogLevel.FromString(expectedNLogLevelName);

        // Act
        var result = testAccessor.TestGetNLogLevel(microsoftLevel);

        // Assert
        _ = result.Should().Be(expectedNLogLevel);
    }

    /// <summary>
    /// Tests that ConfigureNLogProvider sets up logging correctly.
    /// </summary>
    [Fact]
    public void ConfigureNLogProvider_WithValidBuilder_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        _ = mockLoggingBuilder.Setup(x => x.Services).Returns(new ServiceCollection());

        // Act & Assert
        var action = () => testAccessor.TestConfigureNLogProvider(mockLoggingBuilder.Object, MELogLevel.Information);
        _ = action.Should().NotThrow();
    }


    /// <summary>
    /// Tests that ConfigureLogPaths handles service mode correctly.
    /// </summary>
    [Fact]
    public void ConfigureLogPaths_WithServiceMode_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();
        var nlogConfig = new NLog.Config.LoggingConfiguration();

        // Act & Assert
        var action = () => testAccessor.TestConfigureLogPaths(nlogConfig, true);
        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that ConfigureLogPaths handles non-service mode correctly.
    /// </summary>
    [Fact]
    public void ConfigureLogPaths_WithNonServiceMode_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();
        var nlogConfig = new NLog.Config.LoggingConfiguration();

        // Act & Assert
        var action = () => testAccessor.TestConfigureLogPaths(nlogConfig, false);
        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that SetInternalLogFile handles service mode correctly.
    /// </summary>
    [Fact]
    public void SetInternalLogFile_WithServiceMode_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();

        // Act & Assert
        var action = () => testAccessor.TestSetInternalLogFile(true);
        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that SetInternalLogFile handles non-service mode correctly.
    /// </summary>
    [Fact]
    public void SetInternalLogFile_WithNonServiceMode_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();

        // Act & Assert
        var action = () => testAccessor.TestSetInternalLogFile(false);
        _ = action.Should().NotThrow();
    }
}
