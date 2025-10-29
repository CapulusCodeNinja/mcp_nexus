using FluentAssertions;

using Nexus.Config.Models;

using Xunit;

namespace Nexus.Config_unittests.Models;

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
        _ = config.Logging.Should().NotBeNull();
        _ = config.Logging.LogLevel.Should().Be("Information");
        _ = config.McpNexus.Should().NotBeNull();
        _ = config.McpNexus.Server.Should().NotBeNull();
        _ = config.McpNexus.Transport.Should().NotBeNull();
        _ = config.McpNexus.Debugging.Should().NotBeNull();
        _ = config.McpNexus.AutomatedRecovery.Should().NotBeNull();
        _ = config.McpNexus.Service.Should().NotBeNull();
        _ = config.McpNexus.SessionManagement.Should().NotBeNull();
        _ = config.McpNexus.Extensions.Should().NotBeNull();
        _ = config.McpNexus.Batching.Should().NotBeNull();
        _ = config.IpRateLimiting.Should().NotBeNull();
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
        _ = settings.Server.Should().NotBeNull();
        _ = settings.Server.Host.Should().Be("0.0.0.0");
        _ = settings.Server.Port.Should().Be(5511);
        _ = settings.Transport.Should().NotBeNull();
        _ = settings.Transport.Mode.Should().Be("http");
        _ = settings.Transport.ServiceMode.Should().BeTrue();
        _ = settings.Debugging.Should().NotBeNull();
        _ = settings.AutomatedRecovery.Should().NotBeNull();
        _ = settings.Service.Should().NotBeNull();
        _ = settings.SessionManagement.Should().NotBeNull();
        _ = settings.SessionManagement.MaxConcurrentSessions.Should().Be(1000);
        _ = settings.Extensions.Should().NotBeNull();
        _ = settings.Batching.Should().NotBeNull();
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
        _ = settings.Enabled.Should().BeTrue();
        _ = settings.ExtensionsPath.Should().Be("extensions");
        _ = settings.CallbackPort.Should().Be(0);
        _ = settings.GracefulTerminationTimeoutMs.Should().Be(2000);
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
        _ = settings.Enabled.Should().BeTrue();
        _ = settings.MaxBatchSize.Should().Be(5);
        _ = settings.BatchWaitTimeoutMs.Should().Be(2000);
        _ = settings.BatchTimeoutMultiplier.Should().Be(1.0);
        _ = settings.MaxBatchTimeoutMinutes.Should().Be(30);
        _ = settings.ExcludedCommands.Should().NotBeNull();
        _ = settings.ExcludedCommands.Should().BeEmpty();
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
        _ = config.Logging.LogLevel.Should().Be("Debug");
        _ = config.McpNexus.SessionManagement.MaxConcurrentSessions.Should().Be(20);
        _ = config.McpNexus.Debugging.CommandTimeoutMs.Should().Be(600000);
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
        _ = settings.Enabled.Should().BeFalse();
        _ = settings.ExtensionsPath.Should().Be("custom_extensions");
        _ = settings.CallbackPort.Should().Be(8080);
        _ = settings.GracefulTerminationTimeoutMs.Should().Be(5000);
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
        _ = settings.Enabled.Should().BeFalse();
        _ = settings.MaxBatchSize.Should().Be(10);
        _ = settings.BatchWaitTimeoutMs.Should().Be(5000);
        _ = settings.BatchTimeoutMultiplier.Should().Be(2.0);
        _ = settings.MaxBatchTimeoutMinutes.Should().Be(60);
        _ = settings.ExcludedCommands.Should().HaveCount(2);
        _ = settings.ExcludedCommands.Should().Contain("!analyze");
        _ = settings.ExcludedCommands.Should().Contain("!dump");
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
        _ = settings.Host.Should().Be("0.0.0.0");
        _ = settings.Port.Should().Be(5511);
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
        _ = settings.Mode.Should().Be("http");
        _ = settings.ServiceMode.Should().BeTrue();
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
        _ = settings.CdbPath.Should().BeNull();
        _ = settings.CommandTimeoutMs.Should().Be(600000);
        _ = settings.IdleTimeoutMs.Should().Be(300000);
        _ = settings.SymbolServerMaxRetries.Should().Be(1);
        _ = settings.SymbolSearchPath.Should().Be("srv*T:\\symbols*https://symbols.int.avast.com/symbols;srv*T:\\symbols*https://msdl.microsoft.com/download/symbols");
        _ = settings.StartupDelayMs.Should().Be(500);
        _ = settings.OutputReadingTimeoutMs.Should().Be(300000);
        _ = settings.EnableCommandPreprocessing.Should().BeTrue();
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
        _ = settings.MaxConcurrentSessions.Should().Be(1000);
        _ = settings.SessionTimeoutMinutes.Should().Be(30);
        _ = settings.CleanupIntervalSeconds.Should().Be(5);
        _ = settings.DisposalTimeoutSeconds.Should().Be(30);
        _ = settings.DefaultCommandTimeoutMinutes.Should().Be(10);
        _ = settings.MemoryCleanupThresholdMB.Should().Be(1024);
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
        _ = settings.EnableEndpointRateLimiting.Should().BeTrue();
        _ = settings.StackBlockedRequests.Should().BeFalse();
        _ = settings.RealIpHeader.Should().Be("X-Real-IP");
        _ = settings.ClientIdHeader.Should().Be("X-ClientId");
        _ = settings.GeneralRules.Should().NotBeNull();
        _ = settings.GeneralRules.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that AutomatedRecoverySettings has correct default values.
    /// </summary>
    [Fact]
    public void AutomatedRecoverySettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new AutomatedRecoverySettings();

        // Assert
        _ = settings.DefaultCommandTimeoutMinutes.Should().Be(10);
        _ = settings.ComplexCommandTimeoutMinutes.Should().Be(30);
        _ = settings.MaxCommandTimeoutMinutes.Should().Be(60);
        _ = settings.HealthCheckIntervalSeconds.Should().Be(30);
        _ = settings.MaxRecoveryAttempts.Should().Be(3);
        _ = settings.RecoveryDelaySeconds.Should().Be(5);
    }

    /// <summary>
    /// Tests that ServiceSettings has correct default values.
    /// </summary>
    [Fact]
    public void ServiceSettings_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new ServiceSettings();

        // Assert
        _ = settings.InstallPath.Should().Be("C:\\Program Files\\MCP-Nexus");
        _ = settings.BackupPath.Should().Be("C:\\Program Files\\MCP-Nexus\\backups");
        _ = settings.ServiceName.Should().Be("MCP-Nexus");
        _ = settings.DisplayName.Should().Be("MCP-Nexus Debugging Server");
    }

    /// <summary>
    /// Tests that RateLimitRule can be created and modified.
    /// </summary>
    [Fact]
    public void RateLimitRule_CanBeCreatedAndModified()
    {
        // Act
        var rule = new RateLimitRule
        {
            Endpoint = "/api/*",
            Period = "1s",
            Limit = 10
        };

        // Assert
        _ = rule.Endpoint.Should().Be("/api/*");
        _ = rule.Period.Should().Be("1s");
        _ = rule.Limit.Should().Be(10);
    }

    /// <summary>
    /// Tests that LoggingSettings can be modified.
    /// </summary>
    [Fact]
    public void LoggingSettings_CanBeModified()
    {
        // Act
        var settings = new LoggingSettings
        {
            LogLevel = "Debug"
        };

        // Assert
        _ = settings.LogLevel.Should().Be("Debug");
    }

    /// <summary>
    /// Tests that ServerSettings can be modified.
    /// </summary>
    [Fact]
    public void ServerSettings_CanBeModified()
    {
        // Act
        var settings = new ServerSettings
        {
            Host = "localhost",
            Port = 8080
        };

        // Assert
        _ = settings.Host.Should().Be("localhost");
        _ = settings.Port.Should().Be(8080);
    }

    /// <summary>
    /// Tests that TransportSettings can be modified.
    /// </summary>
    [Fact]
    public void TransportSettings_CanBeModified()
    {
        // Act
        var settings = new TransportSettings
        {
            Mode = "stdio",
            ServiceMode = false
        };

        // Assert
        _ = settings.Mode.Should().Be("stdio");
        _ = settings.ServiceMode.Should().BeFalse();
    }

    /// <summary>
    /// Tests that DebuggingSettings can be modified.
    /// </summary>
    [Fact]
    public void DebuggingSettings_CanBeModified()
    {
        // Act
        var settings = new DebuggingSettings
        {
            CdbPath = "C:\\WinDbg\\cdb.exe",
            CommandTimeoutMs = 300000,
            IdleTimeoutMs = 150000,
            SymbolServerMaxRetries = 3,
            StartupDelayMs = 1000,
            EnableCommandPreprocessing = false
        };

        // Assert
        _ = settings.CdbPath.Should().Be("C:\\WinDbg\\cdb.exe");
        _ = settings.CommandTimeoutMs.Should().Be(300000);
        _ = settings.IdleTimeoutMs.Should().Be(150000);
        _ = settings.SymbolServerMaxRetries.Should().Be(3);
        _ = settings.StartupDelayMs.Should().Be(1000);
        _ = settings.EnableCommandPreprocessing.Should().BeFalse();
    }

    /// <summary>
    /// Tests that SessionManagementSettings can be modified.
    /// </summary>
    [Fact]
    public void SessionManagementSettings_CanBeModified()
    {
        // Act
        var settings = new SessionManagementSettings
        {
            MaxConcurrentSessions = 50,
            SessionTimeoutMinutes = 60,
            CleanupIntervalSeconds = 10,
            DisposalTimeoutSeconds = 60,
            DefaultCommandTimeoutMinutes = 20,
            MemoryCleanupThresholdMB = 2048
        };

        // Assert
        _ = settings.MaxConcurrentSessions.Should().Be(50);
        _ = settings.SessionTimeoutMinutes.Should().Be(60);
        _ = settings.CleanupIntervalSeconds.Should().Be(10);
        _ = settings.DisposalTimeoutSeconds.Should().Be(60);
        _ = settings.DefaultCommandTimeoutMinutes.Should().Be(20);
        _ = settings.MemoryCleanupThresholdMB.Should().Be(2048);
    }

    /// <summary>
    /// Tests that AutomatedRecoverySettings can be modified.
    /// </summary>
    [Fact]
    public void AutomatedRecoverySettings_CanBeModified()
    {
        // Act
        var settings = new AutomatedRecoverySettings
        {
            DefaultCommandTimeoutMinutes = 20,
            ComplexCommandTimeoutMinutes = 60,
            MaxCommandTimeoutMinutes = 120,
            HealthCheckIntervalSeconds = 60,
            MaxRecoveryAttempts = 5,
            RecoveryDelaySeconds = 10
        };

        // Assert
        _ = settings.DefaultCommandTimeoutMinutes.Should().Be(20);
        _ = settings.ComplexCommandTimeoutMinutes.Should().Be(60);
        _ = settings.MaxCommandTimeoutMinutes.Should().Be(120);
        _ = settings.HealthCheckIntervalSeconds.Should().Be(60);
        _ = settings.MaxRecoveryAttempts.Should().Be(5);
        _ = settings.RecoveryDelaySeconds.Should().Be(10);
    }

    /// <summary>
    /// Tests that IpRateLimitingSettings can be modified.
    /// </summary>
    [Fact]
    public void IpRateLimitingSettings_CanBeModified()
    {
        // Act
        var settings = new IpRateLimitingSettings
        {
            EnableEndpointRateLimiting = false,
            StackBlockedRequests = true,
            RealIpHeader = "X-Forwarded-For",
            ClientIdHeader = "X-Client"
        };
        settings.GeneralRules.Add(new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 100 });

        // Assert
        _ = settings.EnableEndpointRateLimiting.Should().BeFalse();
        _ = settings.StackBlockedRequests.Should().BeTrue();
        _ = settings.RealIpHeader.Should().Be("X-Forwarded-For");
        _ = settings.ClientIdHeader.Should().Be("X-Client");
        _ = settings.GeneralRules.Should().HaveCount(1);
    }
}
