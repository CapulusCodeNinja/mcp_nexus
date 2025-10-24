using FluentAssertions;

using nexus.extensions.Configuration;

using Xunit;

namespace nexus.extensions_unittests.Configuration;

/// <summary>
/// Unit tests for the ExtensionConfiguration class.
/// </summary>
public class ExtensionConfigurationTests
{
    /// <summary>
    /// Verifies that ExtensionConfiguration can be instantiated with default values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var config = new ExtensionConfiguration();

        // Assert
        config.Enabled.Should().BeTrue();
        config.ExtensionsPath.Should().Be("extensions");
        config.CallbackPort.Should().Be(0);
        config.GracefulTerminationTimeoutMs.Should().Be(2000);
    }

    /// <summary>
    /// Verifies that Enabled property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Enabled_ShouldSetAndGetValue()
    {
        // Arrange
        var config = new ExtensionConfiguration
        {
            // Act
            Enabled = false
        };

        // Assert
        config.Enabled.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ExtensionsPath property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ExtensionsPath_ShouldSetAndGetValue()
    {
        // Arrange
        var config = new ExtensionConfiguration();
        const string path = "C:\\custom\\extensions";

        // Act
        config.ExtensionsPath = path;

        // Assert
        config.ExtensionsPath.Should().Be(path);
    }

    /// <summary>
    /// Verifies that CallbackPort property can be set and retrieved.
    /// </summary>
    [Fact]
    public void CallbackPort_ShouldSetAndGetValue()
    {
        // Arrange
        var config = new ExtensionConfiguration();
        const int port = 8080;

        // Act
        config.CallbackPort = port;

        // Assert
        config.CallbackPort.Should().Be(port);
    }

    /// <summary>
    /// Verifies that GracefulTerminationTimeoutMs property can be set and retrieved.
    /// </summary>
    [Fact]
    public void GracefulTerminationTimeoutMs_ShouldSetAndGetValue()
    {
        // Arrange
        var config = new ExtensionConfiguration();
        const int timeout = 5000;

        // Act
        config.GracefulTerminationTimeoutMs = timeout;

        // Assert
        config.GracefulTerminationTimeoutMs.Should().Be(timeout);
    }

    /// <summary>
    /// Verifies that all properties can be set via object initializer.
    /// </summary>
    [Fact]
    public void ObjectInitializer_ShouldSetAllProperties()
    {
        // Act
        var config = new ExtensionConfiguration
        {
            Enabled = false,
            ExtensionsPath = "/opt/extensions",
            CallbackPort = 9000,
            GracefulTerminationTimeoutMs = 3000
        };

        // Assert
        config.Enabled.Should().BeFalse();
        config.ExtensionsPath.Should().Be("/opt/extensions");
        config.CallbackPort.Should().Be(9000);
        config.GracefulTerminationTimeoutMs.Should().Be(3000);
    }

    /// <summary>
    /// Verifies that CallbackPort can be set to zero to use MCP server port.
    /// </summary>
    [Fact]
    public void CallbackPort_ShouldAllowZeroForMcpServerPort()
    {
        // Arrange
        var config = new ExtensionConfiguration
        {
            CallbackPort = 8080
        };

        // Act
        config.CallbackPort = 0;

        // Assert
        config.CallbackPort.Should().Be(0);
    }

    /// <summary>
    /// Verifies that ExtensionsPath can be set to relative path.
    /// </summary>
    [Fact]
    public void ExtensionsPath_ShouldAllowRelativePath()
    {
        // Arrange
        var config = new ExtensionConfiguration
        {
            // Act
            ExtensionsPath = "custom/extensions"
        };

        // Assert
        config.ExtensionsPath.Should().Be("custom/extensions");
    }

    /// <summary>
    /// Verifies that ExtensionsPath can be set to absolute path.
    /// </summary>
    [Fact]
    public void ExtensionsPath_ShouldAllowAbsolutePath()
    {
        // Arrange
        var config = new ExtensionConfiguration
        {
            // Act
            ExtensionsPath = "C:\\Program Files\\Extensions"
        };

        // Assert
        config.ExtensionsPath.Should().Be("C:\\Program Files\\Extensions");
    }
}

