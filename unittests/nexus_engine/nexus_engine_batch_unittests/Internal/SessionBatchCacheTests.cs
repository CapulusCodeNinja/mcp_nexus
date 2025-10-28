using FluentAssertions;

using Nexus.Engine.Batch.Internal;

using Xunit;

namespace Nexus.Engine.Batch.Tests.Internal;

/// <summary>
/// Unit tests for <see cref="SessionBatchCache"/> class.
/// </summary>
public class SessionBatchCacheTests
{
    #region AddBatch Tests

    /// <summary>
    /// Verifies that AddBatch stores both forward and reverse mappings.
    /// </summary>
    [Fact]
    public void AddBatch_StoresForwardAndReverseMapping()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var batchId = "cmd-sess-1-batch";
        var commandIds = new List<string> { "cmd-sess-1-1", "cmd-sess-1-2", "cmd-sess-1-3" };

        // Act
        cache.AddBatch(batchId, commandIds);

        // Assert - forward mapping
        var retrievedIds = cache.GetOriginalCommandIds(batchId);
        _ = retrievedIds.Should().BeEquivalentTo(commandIds);

        // Assert - reverse mapping
        foreach (var commandId in commandIds)
        {
            var retrievedBatchId = cache.GetBatchCommandId(commandId);
            _ = retrievedBatchId.Should().Be(batchId);
        }
    }

    /// <summary>
    /// Verifies that AddBatch throws ArgumentNullException when batchId is null.
    /// </summary>
    [Fact]
    public void AddBatch_NullBatchId_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var commandIds = new List<string> { "cmd-1", "cmd-2" };

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => cache.AddBatch(null!, commandIds));
    }

    /// <summary>
    /// Verifies that AddBatch throws ArgumentNullException when commandIds is null.
    /// </summary>
    [Fact]
    public void AddBatch_NullCommandIds_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var batchId = "cmd-batch-1";

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => cache.AddBatch(batchId, null!));
    }

    #endregion

    #region GetOriginalCommandIds Tests

    /// <summary>
    /// Verifies that GetOriginalCommandIds returns all command IDs in a batch.
    /// </summary>
    [Fact]
    public void GetOriginalCommandIds_ReturnsAllCommandsInBatch()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var batchId = "cmd-sess-1-batch";
        var commandIds = new List<string> { "cmd-sess-1-1", "cmd-sess-1-2", "cmd-sess-1-3" };
        cache.AddBatch(batchId, commandIds);

        // Act
        var result = cache.GetOriginalCommandIds(batchId);

        // Assert
        _ = result.Should().BeEquivalentTo(commandIds);
        _ = result.Should().NotBeSameAs(commandIds); // Should return a copy
    }

    /// <summary>
    /// Verifies that GetOriginalCommandIds returns single-item list for non-batch command.
    /// </summary>
    [Fact]
    public void GetOriginalCommandIds_NonBatchCommand_ReturnsSingleItemList()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var commandId = "cmd-sess-1-1";

        // Act
        var result = cache.GetOriginalCommandIds(commandId);

        // Assert
        _ = result.Should().HaveCount(1);
        _ = result[0].Should().Be(commandId);
    }

    /// <summary>
    /// Verifies that GetOriginalCommandIds throws ArgumentNullException when batchId is null.
    /// </summary>
    [Fact]
    public void GetOriginalCommandIds_NullBatchId_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SessionBatchCache();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => cache.GetOriginalCommandIds(null!));
    }

    #endregion

    #region GetBatchCommandId Tests

    /// <summary>
    /// Verifies that GetBatchCommandId returns correct batch ID for command in batch.
    /// </summary>
    [Fact]
    public void GetBatchCommandId_ReturnsCorrectBatchId()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var batchId = "cmd-sess-1-batch";
        var commandIds = new List<string> { "cmd-sess-1-1", "cmd-sess-1-2", "cmd-sess-1-3" };
        cache.AddBatch(batchId, commandIds);

        // Act & Assert
        foreach (var commandId in commandIds)
        {
            var result = cache.GetBatchCommandId(commandId);
            _ = result.Should().Be(batchId);
        }
    }

    /// <summary>
    /// Verifies that GetBatchCommandId returns null for command not in any batch.
    /// </summary>
    [Fact]
    public void GetBatchCommandId_NonBatchedCommand_ReturnsNull()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var commandId = "cmd-sess-1-1";

        // Act
        var result = cache.GetBatchCommandId(commandId);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetBatchCommandId throws ArgumentNullException when commandId is null.
    /// </summary>
    [Fact]
    public void GetBatchCommandId_NullCommandId_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new SessionBatchCache();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => cache.GetBatchCommandId(null!));
    }

    #endregion

    #region Clear Tests

    /// <summary>
    /// Verifies that Clear removes all mappings.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllMappings()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var batchId = "cmd-sess-1-batch";
        var commandIds = new List<string> { "cmd-sess-1-1", "cmd-sess-1-2", "cmd-sess-1-3" };
        cache.AddBatch(batchId, commandIds);

        // Act
        cache.Clear();

        // Assert - forward mapping cleared
        var forwardResult = cache.GetOriginalCommandIds(batchId);
        _ = forwardResult.Should().HaveCount(1);
        _ = forwardResult[0].Should().Be(batchId);

        // Assert - reverse mapping cleared
        foreach (var commandId in commandIds)
        {
            var reverseResult = cache.GetBatchCommandId(commandId);
            _ = reverseResult.Should().BeNull();
        }
    }

    /// <summary>
    /// Verifies that Clear can be called on empty cache without error.
    /// </summary>
    [Fact]
    public void Clear_EmptyCache_DoesNotThrow()
    {
        // Arrange
        var cache = new SessionBatchCache();

        // Act & Assert
        cache.Clear(); // Should not throw
    }

    #endregion

    #region Multiple Batch Tests

    /// <summary>
    /// Verifies that multiple batches can coexist in the same cache.
    /// </summary>
    [Fact]
    public void MultipleBatches_CanCoexist()
    {
        // Arrange
        var cache = new SessionBatchCache();
        var batch1Id = "cmd-sess-1-batch1";
        var batch1Commands = new List<string> { "cmd-sess-1-1", "cmd-sess-1-2" };
        var batch2Id = "cmd-sess-1-batch2";
        var batch2Commands = new List<string> { "cmd-sess-1-3", "cmd-sess-1-4" };

        // Act
        cache.AddBatch(batch1Id, batch1Commands);
        cache.AddBatch(batch2Id, batch2Commands);

        // Assert - batch 1
        var result1 = cache.GetOriginalCommandIds(batch1Id);
        _ = result1.Should().BeEquivalentTo(batch1Commands);
        _ = cache.GetBatchCommandId(batch1Commands[0]).Should().Be(batch1Id);
        _ = cache.GetBatchCommandId(batch1Commands[1]).Should().Be(batch1Id);

        // Assert - batch 2
        var result2 = cache.GetOriginalCommandIds(batch2Id);
        _ = result2.Should().BeEquivalentTo(batch2Commands);
        _ = cache.GetBatchCommandId(batch2Commands[0]).Should().Be(batch2Id);
        _ = cache.GetBatchCommandId(batch2Commands[1]).Should().Be(batch2Id);
    }

    #endregion
}

