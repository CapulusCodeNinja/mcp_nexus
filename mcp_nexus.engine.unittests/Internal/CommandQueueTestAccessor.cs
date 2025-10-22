using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.Events;
using mcp_nexus.Engine.Models;
using mcp_nexus.Engine.Internal;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Test accessor for CommandQueue that provides access to protected methods for testing.
/// </summary>
internal class CommandQueueTestAccessor : CommandQueue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandQueueTestAccessor"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="logger">The logger instance.</param>
    internal CommandQueueTestAccessor(string sessionId, DebugEngineConfiguration configuration, ILogger<CommandQueue> logger)
        : base(sessionId, configuration, logger)
    {
    }

    /// <summary>
    /// Gets or sets the CDB session for testing.
    /// </summary>
    internal ICdbSession? TestCdbSession
    {
        get => GetCdbSession();
        set => SetCdbSession(value);
    }

    /// <summary>
    /// Calls the protected ProcessCommandAsync method.
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task TestProcessCommandAsync(QueuedCommand command, CancellationToken cancellationToken = default)
    {
        return ProcessCommandAsync(command, cancellationToken);
    }

    /// <summary>
    /// Calls the protected UpdateCommandState method.
    /// </summary>
    /// <param name="command">The command to update.</param>
    /// <param name="newState">The new state.</param>
    internal void TestUpdateCommandState(QueuedCommand command, CommandState newState)
    {
        UpdateCommandState(command, newState);
    }

    /// <summary>
    /// Calls the protected SetCommandResult method.
    /// </summary>
    /// <param name="command">The command to set result for.</param>
    /// <param name="result">The result to set.</param>
    internal void TestSetCommandResult(QueuedCommand command, CommandInfo result)
    {
        SetCommandResult(command, result);
    }

    /// <summary>
    /// Gets the CDB session using reflection.
    /// </summary>
    /// <returns>The CDB session.</returns>
    private ICdbSession? GetCdbSession()
    {
        var field = typeof(CommandQueue).GetField("m_CdbSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(this) as ICdbSession;
    }

    /// <summary>
    /// Sets the CDB session using reflection.
    /// </summary>
    /// <param name="cdbSession">The CDB session to set.</param>
    private void SetCdbSession(ICdbSession? cdbSession)
    {
        var field = typeof(CommandQueue).GetField("m_CdbSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, cdbSession);
    }
}
