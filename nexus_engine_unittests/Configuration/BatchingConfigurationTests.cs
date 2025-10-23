using Xunit;
using FluentAssertions;
using nexus.engine.Configuration;

namespace nexus.engine.unittests.Configuration;

/// <summary>
/// Unit tests for the BatchingConfiguration class.
/// </summary>
public class BatchingConfigurationTests
{
    /// <summary>
    /// Verifies that the BatchingConfiguration class sets all default values correctly.
    /// </summary>
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var config = new BatchingConfiguration();

        // Assert
        config.Enabled.Should().BeTrue();
        config.MinBatchSize.Should().Be(2);
        config.MaxBatchSize.Should().Be(5);
        config.BatchWaitTimeout.Should().Be(TimeSpan.FromMilliseconds(2000));
        config.BatchTimeoutMultiplier.Should().Be(1.0);
        config.MaxBatchTimeout.Should().Be(TimeSpan.FromMinutes(30));
        config.ExcludedCommands.Should().NotBeNull();
        config.ExcludedCommands.Should().HaveCount(9);
        config.ExcludedCommands.Should().Contain("!analyze");
        config.ExcludedCommands.Should().Contain("!dump");
        config.ExcludedCommands.Should().Contain("!heap");
        config.ExcludedCommands.Should().Contain("!memusage");
        config.ExcludedCommands.Should().Contain("!runaway");
        config.ExcludedCommands.Should().Contain("~*k");
        config.ExcludedCommands.Should().Contain("!locks");
        config.ExcludedCommands.Should().Contain("!cs");
        config.ExcludedCommands.Should().Contain("!gchandles");
    }

    /// <summary>
    /// Verifies that all properties of the BatchingConfiguration class can be set to custom values.
    /// </summary>
    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new BatchingConfiguration();
        var excludedCommands = new List<string> { "custom1", "custom2" };

        // Act
        config.Enabled = false;
        config.MinBatchSize = 3;
        config.MaxBatchSize = 10;
        config.BatchWaitTimeout = TimeSpan.FromSeconds(5);
        config.BatchTimeoutMultiplier = 1.5;
        config.MaxBatchTimeout = TimeSpan.FromMinutes(60);
        config.ExcludedCommands = excludedCommands;

        // Assert
        config.Enabled.Should().BeFalse();
        config.MinBatchSize.Should().Be(3);
        config.MaxBatchSize.Should().Be(10);
        config.BatchWaitTimeout.Should().Be(TimeSpan.FromSeconds(5));
        config.BatchTimeoutMultiplier.Should().Be(1.5);
        config.MaxBatchTimeout.Should().Be(TimeSpan.FromMinutes(60));
        config.ExcludedCommands.Should().BeSameAs(excludedCommands);
    }

    /// <summary>
    /// Verifies that setting ExcludedCommands to null does not throw an exception.
    /// </summary>
    [Fact]
    public void ExcludedCommands_WhenSetToNull_ShouldNotThrow()
    {
        // Arrange
        var config = new BatchingConfiguration();

        // Act & Assert
        var action = () => config.ExcludedCommands = null!;
        action.Should().NotThrow();
    }
}
