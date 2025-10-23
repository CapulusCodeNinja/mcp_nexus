using Xunit;
using nexus.engine.Configuration;
using nexus.engine.Models;

namespace nexus.engine.unittests.TestHelpers;

/// <summary>
/// Builder class for creating test data objects.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a default debug engine configuration for testing.
    /// </summary>
    /// <returns>A debug engine configuration with test-friendly defaults.</returns>
    public static DebugEngineConfiguration CreateDebugEngineConfiguration()
    {
        return new DebugEngineConfiguration
        {
            CdbPath = @"C:\TestDebuggers\cdb.exe",
            DefaultCommandTimeout = TimeSpan.FromSeconds(30),
            MaxConcurrentSessions = 3,
            SessionInitializationTimeout = TimeSpan.FromSeconds(10),
            SessionCleanupTimeout = TimeSpan.FromSeconds(5),
            HeartbeatInterval = TimeSpan.FromSeconds(5),
            MaxQueuedCommandsPerSession = 100,
            MaxCachedResultsPerSession = 1000,
            Batching = new BatchingConfiguration
            {
                Enabled = false, // Disable batching for simpler tests
                MinBatchSize = 2,
                MaxBatchSize = 5,
                BatchWaitTimeout = TimeSpan.FromMilliseconds(1000),
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeout = TimeSpan.FromMinutes(5),
                ExcludedCommands = new List<string> { "!analyze", "!dump" }
            }
        };
    }

    /// <summary>
    /// Creates a command info for a queued command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="command">The command text.</param>
    /// <returns>A queued command info.</returns>
    public static CommandInfo CreateQueuedCommandInfo(string commandId = "cmd-test", string command = "lm")
    {
        return CommandInfo.Queued(commandId, command, DateTime.Now);
    }

    /// <summary>
    /// Creates a command info for an executing command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="command">The command text.</param>
    /// <returns>An executing command info.</returns>
    public static CommandInfo CreateExecutingCommandInfo(string commandId = "cmd-test", string command = "lm")
    {
        var queuedTime = DateTime.Now.AddSeconds(-1);
        var startTime = DateTime.Now;
        return CommandInfo.Executing(commandId, command, queuedTime, startTime);
    }

    /// <summary>
    /// Creates a command info for a completed command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="command">The command text.</param>
    /// <param name="output">The command output.</param>
    /// <param name="isSuccess">Whether the command succeeded.</param>
    /// <returns>A completed command info.</returns>
    public static CommandInfo CreateCompletedCommandInfo(
        string commandId = "cmd-test", 
        string command = "lm", 
        string output = "Test output", 
        bool isSuccess = true)
    {
        var queuedTime = DateTime.Now.AddSeconds(-5);
        var startTime = DateTime.Now.AddSeconds(-4);
        var endTime = DateTime.Now;
        return CommandInfo.Completed(commandId, command, queuedTime, startTime, endTime, output, isSuccess);
    }

    /// <summary>
    /// Creates a command info for a cancelled command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="command">The command text.</param>
    /// <returns>A cancelled command info.</returns>
    public static CommandInfo CreateCancelledCommandInfo(string commandId = "cmd-test", string command = "lm")
    {
        var queuedTime = DateTime.Now.AddSeconds(-3);
        var startTime = DateTime.Now.AddSeconds(-2);
        var endTime = DateTime.Now;
        return CommandInfo.Cancelled(commandId, command, queuedTime, startTime, endTime);
    }

    /// <summary>
    /// Creates a command info for a timed out command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="command">The command text.</param>
    /// <returns>A timed out command info.</returns>
    public static CommandInfo CreateTimedOutCommandInfo(string commandId = "cmd-test", string command = "lm")
    {
        var queuedTime = DateTime.Now.AddSeconds(-10);
        var startTime = DateTime.Now.AddSeconds(-9);
        var endTime = DateTime.Now;
        return CommandInfo.TimedOut(commandId, command, queuedTime, startTime, endTime, "Command timed out");
    }
}
