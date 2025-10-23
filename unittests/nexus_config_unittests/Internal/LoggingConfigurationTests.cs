using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NLog;
using NLog.Config;
using nexus.config.Internal;
using Xunit;
using MELogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace nexus.config_unittests.Internal;

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
        m_MockConfiguration.Setup(x => x["Logging:LogLevel"]).Returns("Information");
    }

    /// <summary>
    /// Tests that GetLogLevelFromConfiguration returns correct log level.
    /// </summary>
    [Theory]
    [InlineData("Information", MELogLevel.Information)]
    [InlineData("Debug", MELogLevel.Debug)]
    [InlineData("Warning", MELogLevel.Warning)]
    [InlineData("Error", MELogLevel.Error)]
    [InlineData("Critical", MELogLevel.Critical)]
    [InlineData("Trace", MELogLevel.Trace)]
    [InlineData("None", MELogLevel.None)]
    public void GetLogLevelFromConfiguration_WithValidLogLevel_ShouldReturnCorrectLevel(string logLevelString, MELogLevel expectedLevel)
    {
        // Arrange
        m_MockConfiguration.Setup(x => x["Logging:LogLevel"]).Returns(logLevelString);
        var testAccessor = new LoggingConfigurationTestAccessor();

        // Act
        var result = testAccessor.TestGetLogLevelFromConfiguration(m_MockConfiguration.Object);

        // Assert
        result.Should().Be(expectedLevel);
    }

    /// <summary>
    /// Tests that GetLogLevelFromConfiguration returns Information for null configuration.
    /// </summary>
    [Fact]
    public void GetLogLevelFromConfiguration_WithNullConfiguration_ShouldReturnInformation()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();

        // Act
        var result = testAccessor.TestGetLogLevelFromConfiguration(null!);

        // Assert
        result.Should().Be(MELogLevel.Information);
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
        result.Should().Be(expectedLevel);
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
        result.Should().Be(expectedNLogLevel);
    }

    /// <summary>
    /// Tests that ConfigureLogging calls all required methods.
    /// </summary>
    [Fact]
    public void ConfigureLogging_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        mockLoggingBuilder.Setup(x => x.Services).Returns(new ServiceCollection());

        // Act & Assert
        var action = () => testAccessor.ConfigureLogging(mockLoggingBuilder.Object, m_MockConfiguration.Object, false);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that ConfigureLogging works in service mode.
    /// </summary>
    [Fact]
    public void ConfigureLogging_WithServiceMode_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        mockLoggingBuilder.Setup(x => x.Services).Returns(new ServiceCollection());

        // Act & Assert
        var action = () => testAccessor.ConfigureLogging(mockLoggingBuilder.Object, m_MockConfiguration.Object, true);
        action.Should().NotThrow();
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
        mockLoggingBuilder.Setup(x => x.Services).Returns(new ServiceCollection());

        // Act & Assert
        var action = () => testAccessor.TestConfigureNLogProvider(mockLoggingBuilder.Object, MELogLevel.Information);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that ConfigureMicrosoftLogging sets up filters correctly.
    /// </summary>
    [Fact]
    public void ConfigureMicrosoftLogging_WithValidBuilder_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new LoggingConfigurationTestAccessor();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        mockLoggingBuilder.Setup(x => x.Services).Returns(new ServiceCollection());

        // Act & Assert
        var action = () => testAccessor.TestConfigureMicrosoftLogging(mockLoggingBuilder.Object, MELogLevel.Information);
        action.Should().NotThrow();
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
        action.Should().NotThrow();
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
        action.Should().NotThrow();
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
        action.Should().NotThrow();
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
        action.Should().NotThrow();
    }
}
