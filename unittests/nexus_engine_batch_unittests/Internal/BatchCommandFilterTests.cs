using Microsoft.Extensions.Logging;
using Moq;
using nexus.engine.batch.Configuration;
using nexus.engine.batch.Internal;
using Xunit;

namespace nexus.engine.batch.tests;

/// <summary>
/// Tests for <see cref="BatchCommandFilter"/>.
/// </summary>
public class BatchCommandFilterTests
{
    private readonly Mock<ILogger<BatchCommandFilter>> m_MockLogger;
    private readonly BatchingConfiguration m_Configuration;
    private readonly BatchCommandFilter m_Filter;

    public BatchCommandFilterTests()
    {
        m_MockLogger = new Mock<ILogger<BatchCommandFilter>>();
        m_Configuration = new BatchingConfiguration
        {
            Enabled = true,
            MinBatchSize = 2,
            MaxBatchSize = 5,
            ExcludedCommands = new List<string> { "!analyze", "!dump", "~*k" }
        };
        m_Filter = new BatchCommandFilter(m_Configuration, m_MockLogger.Object);
    }

    [Fact]
    public void ShouldBatch_WhenBatchingEnabled_AndEnoughCommands_ReturnsTrue()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" }
        };

        // Act
        var result = m_Filter.ShouldBatch(commands);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldBatch_WhenBatchingDisabled_ReturnsFalse()
    {
        // Arrange
        m_Configuration.Enabled = false;
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" }
        };

        // Act
        var result = m_Filter.ShouldBatch(commands);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldBatch_WhenNotEnoughCommands_ReturnsFalse()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" }
        };

        // Act
        var result = m_Filter.ShouldBatch(commands);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldBatch_WhenCommandExcluded_ReturnsFalse()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!analyze -v" }
        };

        // Act
        var result = m_Filter.ShouldBatch(commands);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldBatch_WhenEmptyList_ReturnsFalse()
    {
        // Arrange
        var commands = new List<Command>();

        // Act
        var result = m_Filter.ShouldBatch(commands);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCommandExcluded_WithExactMatch_ReturnsTrue()
    {
        // Arrange
        var commandText = "!analyze";

        // Act
        var result = m_Filter.IsCommandExcluded(commandText);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCommandExcluded_WithPrefixMatch_ReturnsTrue()
    {
        // Arrange
        var commandText = "!analyze -v";

        // Act
        var result = m_Filter.IsCommandExcluded(commandText);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCommandExcluded_WithNonExcluded_ReturnsFalse()
    {
        // Arrange
        var commandText = "lm";

        // Act
        var result = m_Filter.IsCommandExcluded(commandText);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCommandExcluded_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var commandText = "";

        // Act
        var result = m_Filter.IsCommandExcluded(commandText);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCommandExcluded_WithWhitespace_ReturnsFalse()
    {
        // Arrange
        var commandText = "   ";

        // Act
        var result = m_Filter.IsCommandExcluded(commandText);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCommandExcluded_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var commandText = "!ANALYZE";

        // Act
        var result = m_Filter.IsCommandExcluded(commandText);

        // Assert
        Assert.True(result);
    }
}

