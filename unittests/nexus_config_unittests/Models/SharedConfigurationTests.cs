using FluentAssertions;

using nexus.config.Models;

using Xunit;

namespace nexus.config_unittests.Models;

/// <summary>
/// Unit tests for SharedConfiguration models.
/// </summary>
public class SharedConfigurationTests
{
    /// <summary>
    /// Tests that SharedConfiguration has correct default values.
    /// </summary>
    [Fact]
    public void SharedConfiguration_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var config = new SharedConfiguration();

        // Assert
        config.Logging.Should().NotBeNull();
        config.Logging.LogLevel.Should().Be("Information");
        config.McpNexus.Should().NotBeNull();
        config.McpNexus.Server.Should().NotBeNull();
        config.McpNexus.Transport.Should().NotBeNull();
        config.McpNexus.Debugging.Should().NotBeNull();
        config.McpNexus.AutomatedRecovery.Should().NotBeNull();
        config.McpNexus.Service.Should().NotBeNull();
        config.McpNexus.SessionManagement.Should().NotBeNull();
        config.McpNexus.Extensions.Should().NotBeNull();
        config.McpNexus.Batching.Should().NotBeNull();
        config.IpRateLimiting.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that McpNexusSettings has correct default values.
    /// </summary>
    [Fact]
    public void McpNexusSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new McpNexusSettings();

        // Assert
        settings.Server.Should().NotBeNull();
        settings.Server.Host.Should().Be("0.0.0.0");
        settings.Server.Port.Should().Be(5511);
        settings.Transport.Should().NotBeNull();
        settings.Transport.Mode.Should().Be("http");
        settings.Transport.ServiceMode.Should().BeTrue();
        settings.Debugging.Should().NotBeNull();
        settings.AutomatedRecovery.Should().NotBeNull();
        settings.Service.Should().NotBeNull();
        settings.SessionManagement.Should().NotBeNull();
        settings.SessionManagement.MaxConcurrentSessions.Should().Be(1000);
        settings.Extensions.Should().NotBeNull();
        settings.Batching.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that ExtensionsSettings has correct default values.
    /// </summary>
    [Fact]
    public void ExtensionsSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new ExtensionsSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.ExtensionsPath.Should().Be("extensions");
        settings.CallbackPort.Should().Be(0);
        settings.GracefulTerminationTimeoutMs.Should().Be(2000);
    }

    /// <summary>
    /// Tests that BatchingSettings has correct default values.
    /// </summary>
    [Fact]
    public void BatchingSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new BatchingSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.MaxBatchSize.Should().Be(5);
        settings.BatchWaitTimeoutMs.Should().Be(2000);
        settings.BatchTimeoutMultiplier.Should().Be(1.0);
        settings.MaxBatchTimeoutMinutes.Should().Be(30);
        settings.ExcludedCommands.Should().NotBeNull();
        settings.ExcludedCommands.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that SharedConfiguration can be modified.
    /// </summary>
    [Fact]
    public void SharedConfiguration_CanBeModified_ShouldWork()
    {
        // Arrange
        var config = new SharedConfiguration();

        // Act
        config.Logging.LogLevel = "Debug";
        config.McpNexus.SessionManagement.MaxConcurrentSessions = 20;
        config.McpNexus.Debugging.CommandTimeoutMs = 600000;

        // Assert
        config.Logging.LogLevel.Should().Be("Debug");
        config.McpNexus.SessionManagement.MaxConcurrentSessions.Should().Be(20);
        config.McpNexus.Debugging.CommandTimeoutMs.Should().Be(600000);
    }

    /// <summary>
    /// Tests that ExtensionsSettings can be modified.
    /// </summary>
    [Fact]
    public void ExtensionsSettings_CanBeModified_ShouldWork()
    {
        // Arrange
        var settings = new ExtensionsSettings
        {
            // Act
            Enabled = false,
            ExtensionsPath = "custom_extensions",
            CallbackPort = 8080,
            GracefulTerminationTimeoutMs = 5000
        };

        // Assert
        settings.Enabled.Should().BeFalse();
        settings.ExtensionsPath.Should().Be("custom_extensions");
        settings.CallbackPort.Should().Be(8080);
        settings.GracefulTerminationTimeoutMs.Should().Be(5000);
    }

    /// <summary>
    /// Tests that BatchingSettings can be modified.
    /// </summary>
    [Fact]
    public void BatchingSettings_CanBeModified_ShouldWork()
    {
        // Arrange
        var settings = new BatchingSettings
        {
            // Act
            Enabled = false,
            MaxBatchSize = 10,
            BatchWaitTimeoutMs = 5000,
            BatchTimeoutMultiplier = 2.0,
            MaxBatchTimeoutMinutes = 60
        };
        settings.ExcludedCommands.Add("!analyze");
        settings.ExcludedCommands.Add("!dump");

        // Assert
        settings.Enabled.Should().BeFalse();
        settings.MaxBatchSize.Should().Be(10);
        settings.BatchWaitTimeoutMs.Should().Be(5000);
        settings.BatchTimeoutMultiplier.Should().Be(2.0);
        settings.MaxBatchTimeoutMinutes.Should().Be(60);
        settings.ExcludedCommands.Should().HaveCount(2);
        settings.ExcludedCommands.Should().Contain("!analyze");
        settings.ExcludedCommands.Should().Contain("!dump");
    }

    /// <summary>
    /// Tests that ServerSettings has correct default values.
    /// </summary>
    [Fact]
    public void ServerSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new ServerSettings();

        // Assert
        settings.Host.Should().Be("0.0.0.0");
        settings.Port.Should().Be(5511);
    }

    /// <summary>
    /// Tests that TransportSettings has correct default values.
    /// </summary>
    [Fact]
    public void TransportSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new TransportSettings();

        // Assert
        settings.Mode.Should().Be("http");
        settings.ServiceMode.Should().BeTrue();
    }

    /// <summary>
    /// Tests that DebuggingSettings has correct default values.
    /// </summary>
    [Fact]
    public void DebuggingSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new DebuggingSettings();

        // Assert
        settings.CdbPath.Should().BeNull();
        settings.CommandTimeoutMs.Should().Be(600000);
        settings.IdleTimeoutMs.Should().Be(300000);
        settings.SymbolServerMaxRetries.Should().Be(1);
        settings.SymbolSearchPath.Should().Be("srv*T:\\symbols*https://symbols.int.avast.com/symbols;srv*T:\\symbols*https://msdl.microsoft.com/download/symbols");
        settings.StartupDelayMs.Should().Be(500);
        settings.OutputReadingTimeoutMs.Should().Be(300000);
        settings.EnableCommandPreprocessing.Should().BeTrue();
    }

    /// <summary>
    /// Tests that SessionManagementSettings has correct default values.
    /// </summary>
    [Fact]
    public void SessionManagementSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new SessionManagementSettings();

        // Assert
        settings.MaxConcurrentSessions.Should().Be(1000);
        settings.SessionTimeoutMinutes.Should().Be(30);
        settings.CleanupIntervalMinutes.Should().Be(5);
        settings.DisposalTimeoutSeconds.Should().Be(30);
        settings.DefaultCommandTimeoutMinutes.Should().Be(10);
        settings.MemoryCleanupThresholdMB.Should().Be(1024);
    }

    /// <summary>
    /// Tests that IpRateLimitingSettings has correct default values.
    /// </summary>
    [Fact]
    public void IpRateLimitingSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new IpRateLimitingSettings();

        // Assert
        settings.EnableEndpointRateLimiting.Should().BeTrue();
        settings.StackBlockedRequests.Should().BeFalse();
        settings.RealIpHeader.Should().Be("X-Real-IP");
        settings.ClientIdHeader.Should().Be("X-ClientId");
        settings.GeneralRules.Should().NotBeNull();
        settings.GeneralRules.Should().BeEmpty();
    }
}
