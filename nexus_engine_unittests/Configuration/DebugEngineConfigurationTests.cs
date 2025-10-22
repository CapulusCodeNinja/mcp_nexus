using Xunit;
using FluentAssertions;
using nexus.engine.Configuration;

namespace nexus.engine.unittests.Configuration;

/// <summary>
/// Unit tests for the DebugEngineConfiguration class.
/// </summary>
public class DebugEngineConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var config = new DebugEngineConfiguration();

        // Assert
        config.CdbPath.Should().BeNull();
        config.DefaultCommandTimeout.Should().Be(TimeSpan.FromMinutes(5));
        config.MaxConcurrentSessions.Should().Be(10);
        config.SessionInitializationTimeout.Should().Be(TimeSpan.FromMinutes(2));
        config.SessionCleanupTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(30));
        config.Batching.Should().NotBeNull();
        config.MaxQueuedCommandsPerSession.Should().Be(1000);
        config.MaxCachedResultsPerSession.Should().Be(10000);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new DebugEngineConfiguration();
        var batchingConfig = new BatchingConfiguration();

        // Act
        config.CdbPath = @"C:\Debuggers\cdb.exe";
        config.DefaultCommandTimeout = TimeSpan.FromMinutes(10);
        config.MaxConcurrentSessions = 20;
        config.SessionInitializationTimeout = TimeSpan.FromMinutes(5);
        config.SessionCleanupTimeout = TimeSpan.FromMinutes(1);
        config.HeartbeatInterval = TimeSpan.FromSeconds(60);
        config.Batching = batchingConfig;
        config.MaxQueuedCommandsPerSession = 2000;
        config.MaxCachedResultsPerSession = 20000;

        // Assert
        config.CdbPath.Should().Be(@"C:\Debuggers\cdb.exe");
        config.DefaultCommandTimeout.Should().Be(TimeSpan.FromMinutes(10));
        config.MaxConcurrentSessions.Should().Be(20);
        config.SessionInitializationTimeout.Should().Be(TimeSpan.FromMinutes(5));
        config.SessionCleanupTimeout.Should().Be(TimeSpan.FromMinutes(1));
        config.HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(60));
        config.Batching.Should().BeSameAs(batchingConfig);
        config.MaxQueuedCommandsPerSession.Should().Be(2000);
        config.MaxCachedResultsPerSession.Should().Be(20000);
    }

    [Fact]
    public void Batching_WhenSetToNull_ShouldNotThrow()
    {
        // Arrange
        var config = new DebugEngineConfiguration();

        // Act & Assert
        var action = () => config.Batching = null!;
        action.Should().NotThrow();
    }
}
