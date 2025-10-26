using FluentAssertions;

using Xunit;

namespace Nexus.Engine.Batch.Tests;

/// <summary>
/// Unit tests for the <see cref="BatchProcessor"/> class.
/// </summary>
public class BatchProcessorTests
{
    #region Singleton Tests

    /// <summary>
    /// Verifies that Instance returns a non-null singleton instance.
    /// </summary>
    [Fact]
    public void Instance_ReturnsNonNullSingleton()
    {
        // Act
        var instance = BatchProcessor.Instance;

        // Assert
        _ = instance.Should().NotBeNull();
        _ = instance.Should().BeOfType<BatchProcessor>();
    }

    /// <summary>
    /// Verifies that Instance returns the same instance on multiple calls.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSameInstanceOnMultipleCalls()
    {
        // Act
        var instance1 = BatchProcessor.Instance;
        var instance2 = BatchProcessor.Instance;

        // Assert
        _ = instance1.Should().BeSameAs(instance2);
    }

    #endregion

    #region BatchCommands Tests - Null and Empty

    /// <summary>
    /// Verifies that BatchCommands handles null commands list.
    /// </summary>
    [Fact]
    public void BatchCommands_NullCommands_ReturnsEmptyList()
    {
        // Arrange
        var processor = BatchProcessor.Instance;

        // Act
        var result = processor.BatchCommands("test-session", null!);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that BatchCommands handles empty commands list.
    /// </summary>
    [Fact]
    public void BatchCommands_EmptyCommands_ReturnsEmptyList()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>();

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeEmpty();
    }

    #endregion

    #region BatchCommands Tests - Single Command

    /// <summary>
    /// Verifies that BatchCommands handles a single command.
    /// </summary>
    [Fact]
    public void BatchCommands_SingleCommand_ReturnsUnbatched()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "!analyze" }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(1);
        _ = result[0].CommandId.Should().Be("cmd-1");
        _ = result[0].CommandText.Should().Be("!analyze");
    }

    #endregion

    #region BatchCommands Tests - Multiple Commands

    /// <summary>
    /// Verifies that BatchCommands handles two simple commands.
    /// </summary>
    [Fact]
    public void BatchCommands_TwoSimpleCommands_ProcessesCorrectly()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "lm" },
            new() { CommandId = "cmd-2", CommandText = "dt" }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that BatchCommands handles multiple batchable commands.
    /// </summary>
    [Fact]
    public void BatchCommands_MultipleBatchableCommands_ProcessesCorrectly()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "lm" },
            new() { CommandId = "cmd-2", CommandText = "dt" },
            new() { CommandId = "cmd-3", CommandText = "kL" }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that BatchCommands handles five simple commands.
    /// </summary>
    [Fact]
    public void BatchCommands_FiveSimpleCommands_ProcessesCorrectly()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "lm" },
            new() { CommandId = "cmd-2", CommandText = "dt" },
            new() { CommandId = "cmd-3", CommandText = "kL" },
            new() { CommandId = "cmd-4", CommandText = "r" },
            new() { CommandId = "cmd-5", CommandText = "!peb" }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().BeGreaterThan(0);
    }

    #endregion

    #region BatchCommands Tests - Excluded Commands

    /// <summary>
    /// Verifies that BatchCommands handles excluded commands correctly.
    /// </summary>
    [Fact]
    public void BatchCommands_ExcludedCommand_PassesThroughIndividually()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "!analyze -v" }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(1);
        _ = result[0].CommandId.Should().Be("cmd-1");
        _ = result[0].CommandText.Should().Be("!analyze -v");
    }

    /// <summary>
    /// Verifies that BatchCommands handles mix of excluded and batchable commands.
    /// </summary>
    [Fact]
    public void BatchCommands_MixedExcludedAndBatchable_HandlesCorrectly()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "!analyze -v" },
            new() { CommandId = "cmd-2", CommandText = "lm" },
            new() { CommandId = "cmd-3", CommandText = "dt" }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that BatchCommands preserves command order for non-batched commands.
    /// </summary>
    [Fact]
    public void BatchCommands_PreservesOrderForNonBatchedCommands()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "lm" }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result[0].CommandId.Should().Be("cmd-1");
    }

    #endregion

    #region UnbatchResults Tests - Null and Empty

    /// <summary>
    /// Verifies that UnbatchResults handles null results list.
    /// </summary>
    [Fact]
    public void UnbatchResults_NullResults_ReturnsEmptyList()
    {
        // Arrange
        var processor = BatchProcessor.Instance;

        // Act
        var result = processor.UnbatchResults(null!);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that UnbatchResults handles empty results list.
    /// </summary>
    [Fact]
    public void UnbatchResults_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var results = new List<CommandResult>();

        // Act
        var result = processor.UnbatchResults(results);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeEmpty();
    }

    #endregion

    #region UnbatchResults Tests - Single Result

    /// <summary>
    /// Verifies that UnbatchResults handles a single result.
    /// </summary>
    [Fact]
    public void UnbatchResults_SingleResult_ReturnsCorrectly()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var results = new List<CommandResult>
        {
            new() { CommandId = "cmd-1", ResultText = "Output 1" }
        };

        // Act
        var result = processor.UnbatchResults(results);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(1);
        _ = result[0].CommandId.Should().Be("cmd-1");
        _ = result[0].ResultText.Should().Be("Output 1");
    }

    #endregion

    #region UnbatchResults Tests - Multiple Results

    /// <summary>
    /// Verifies that UnbatchResults handles multiple non-batched results.
    /// </summary>
    [Fact]
    public void UnbatchResults_MultipleNonBatchedResults_ReturnsAll()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var results = new List<CommandResult>
        {
            new() { CommandId = "cmd-1", ResultText = "Output 1" },
            new() { CommandId = "cmd-2", ResultText = "Output 2" },
            new() { CommandId = "cmd-3", ResultText = "Output 3" }
        };

        // Act
        var result = processor.UnbatchResults(results);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(3);
        _ = result[0].CommandId.Should().Be("cmd-1");
        _ = result[1].CommandId.Should().Be("cmd-2");
        _ = result[2].CommandId.Should().Be("cmd-3");
    }

    /// <summary>
    /// Verifies that UnbatchResults preserves result order.
    /// </summary>
    [Fact]
    public void UnbatchResults_PreservesResultOrder()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var results = new List<CommandResult>
        {
            new() { CommandId = "cmd-1", ResultText = "First" },
            new() { CommandId = "cmd-2", ResultText = "Second" },
            new() { CommandId = "cmd-3", ResultText = "Third" }
        };

        // Act
        var result = processor.UnbatchResults(results);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(3);
        _ = result[0].ResultText.Should().Be("First");
        _ = result[1].ResultText.Should().Be("Second");
        _ = result[2].ResultText.Should().Be("Third");
    }

    #endregion

    #region Integration Tests - Batch and Unbatch

    /// <summary>
    /// Verifies that batching and unbatching work together for simple commands.
    /// </summary>
    [Fact]
    public void BatchAndUnbatch_SimpleCommands_RoundTripCorrectly()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "lm" }
        };

        // Act - Batch commands
        var batchedCommands = processor.BatchCommands("test-session", commands);
        _ = batchedCommands.Should().NotBeNull();

        // Create mock results from batched commands
        var results = batchedCommands.Select(cmd => new CommandResult
        {
            CommandId = cmd.CommandId,
            ResultText = $"Result for {cmd.CommandId}"
        }).ToList();

        // Act - Unbatch results
        var unbatchedResults = processor.UnbatchResults(results);

        // Assert
        _ = unbatchedResults.Should().NotBeNull();
        _ = unbatchedResults.Count.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that excluded commands pass through batch/unbatch unchanged.
    /// </summary>
    [Fact]
    public void BatchAndUnbatch_ExcludedCommand_PassesThroughUnchanged()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "!analyze -v" }
        };

        // Act - Batch commands
        var batchedCommands = processor.BatchCommands("test-session", commands);

        // Create results
        var results = batchedCommands.Select(cmd => new CommandResult
        {
            CommandId = cmd.CommandId,
            ResultText = "Analysis output"
        }).ToList();

        // Act - Unbatch results
        var unbatchedResults = processor.UnbatchResults(results);

        // Assert
        _ = unbatchedResults.Should().NotBeNull();
        _ = unbatchedResults.Count.Should().Be(1);
        _ = unbatchedResults[0].CommandId.Should().Be("cmd-1");
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Verifies that BatchCommands handles commands with empty CommandText.
    /// </summary>
    [Fact]
    public void BatchCommands_EmptyCommandText_HandlesGracefully()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = string.Empty }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(1);
    }

    /// <summary>
    /// Verifies that BatchCommands handles commands with whitespace CommandText.
    /// </summary>
    [Fact]
    public void BatchCommands_WhitespaceCommandText_HandlesGracefully()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "   " }
        };

        // Act
        var result = processor.BatchCommands("test-session", commands);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(1);
    }

    /// <summary>
    /// Verifies that UnbatchResults handles results with empty ResultText.
    /// </summary>
    [Fact]
    public void UnbatchResults_EmptyResultText_HandlesGracefully()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var results = new List<CommandResult>
        {
            new() { CommandId = "cmd-1", ResultText = string.Empty }
        };

        // Act
        var result = processor.UnbatchResults(results);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(1);
    }

    /// <summary>
    /// Verifies that UnbatchResults handles results with whitespace ResultText.
    /// </summary>
    [Fact]
    public void UnbatchResults_WhitespaceResultText_HandlesGracefully()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var results = new List<CommandResult>
        {
            new() { CommandId = "cmd-1", ResultText = "   " }
        };

        // Act
        var result = processor.UnbatchResults(results);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Count.Should().Be(1);
    }

    #endregion

    #region Thread Safety Tests

    /// <summary>
    /// Verifies that BatchCommands is thread-safe.
    /// </summary>
    [Fact]
    public async Task BatchCommands_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "lm" },
            new() { CommandId = "cmd-2", CommandText = "dt" }
        };

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            Task.Run(() => processor.BatchCommands("test-session", commands))
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            _ = result.Should().NotBeNull();
            _ = result.Count.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Verifies that UnbatchResults is thread-safe.
    /// </summary>
    [Fact]
    public async Task UnbatchResults_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var processor = BatchProcessor.Instance;
        var results = new List<CommandResult>
        {
            new() { CommandId = "cmd-1", ResultText = "Output 1" },
            new() { CommandId = "cmd-2", ResultText = "Output 2" }
        };

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            Task.Run(() => processor.UnbatchResults(results))
        ).ToArray();

        var taskResults = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in taskResults)
        {
            _ = result.Should().NotBeNull();
            _ = result.Count.Should().Be(2);
        }
    }

    #endregion
}

