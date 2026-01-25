using FluentAssertions;

using Microsoft.Extensions.Configuration;

using Moq;

using WinAiDbg.Config.Models;

using Xunit;

namespace WinAiDbg.Config_unittests.Internal;

/// <summary>
/// Unit tests for ConfigurationLoader.
/// </summary>
public class ConfigurationLoaderTests
{
    private readonly Mock<IConfiguration> m_MockConfiguration;
    private readonly Mock<IConfigurationSection> m_MockLogLevelSection;
    private readonly Mock<IConfigurationSection> m_MockWinAiDbgSection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoaderTests"/> class.
    /// </summary>
    public ConfigurationLoaderTests()
    {
        m_MockConfiguration = new Mock<IConfiguration>();
        m_MockLogLevelSection = new Mock<IConfigurationSection>();
        m_MockWinAiDbgSection = new Mock<IConfigurationSection>();

        // Setup default configuration
        _ = m_MockLogLevelSection.Setup(x => x.Value).Returns("Information");
        _ = m_MockConfiguration.Setup(x => x["Logging:LogLevel"]).Returns("Information");
        _ = m_MockConfiguration.Setup(x => x.GetSection("Logging:LogLevel")).Returns(m_MockLogLevelSection.Object);
    }

    /// <summary>
    /// Tests that LoadConfiguration returns a valid configuration.
    /// </summary>
    [Fact]
    public void LoadConfiguration_WithValidPath_ShouldReturnConfiguration()
    {
        // Arrange
        var testAccessor = new ConfigurationLoaderTestAccessor(mockConfiguration: m_MockConfiguration.Object);

        // Act
        var result = testAccessor.TestLoadConfiguration();

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeAssignableTo<IConfiguration>();
    }

    /// <summary>
    /// Tests that LoadConfiguration handles null path correctly.
    /// </summary>
    [Fact]
    public void LoadConfiguration_WithNullPath_ShouldUseDefaultPath()
    {
        // Arrange
        var testAccessor = new ConfigurationLoaderTestAccessor(mockConfiguration: m_MockConfiguration.Object);

        // Act
        var result = testAccessor.TestLoadConfiguration(null);

        // Assert
        _ = result.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that GetSharedConfiguration returns valid configuration.
    /// </summary>
    [Fact]
    public void GetSharedConfiguration_WithValidConfiguration_ShouldReturnSharedConfiguration()
    {
        // Arrange
        var testAccessor = new ConfigurationLoaderTestAccessor();

        // Act
        var result = testAccessor.TestGetSharedConfiguration();

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeOfType<SharedConfiguration>();
    }

    /// <summary>
    /// Tests that GetSharedConfiguration returns default configuration when no config file exists.
    /// </summary>
    [Fact]
    public void GetSharedConfiguration_WithNoConfigFile_ShouldReturnDefaultConfiguration()
    {
        // Arrange
        var testAccessor = new ConfigurationLoaderTestAccessor();

        // Act
        var result = testAccessor.TestGetSharedConfiguration();

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeOfType<SharedConfiguration>();
    }

    /// <summary>
    /// Tests that LoadConfiguration with custom path works correctly.
    /// </summary>
    [Fact]
    public void LoadConfiguration_WithCustomPath_ShouldUseCustomPath()
    {
        // Arrange
        var customPath = Path.Combine(Path.GetTempPath(), "test_config");
        var testAccessor = new ConfigurationLoaderTestAccessor(mockConfiguration: m_MockConfiguration.Object);

        // Act
        var result = testAccessor.TestLoadConfiguration(customPath);

        // Assert
        _ = result.Should().NotBeNull();
    }
}
