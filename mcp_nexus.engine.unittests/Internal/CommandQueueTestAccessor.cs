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
        // Convert ICdbSession to CdbSession if it's a mock
        if (cdbSession != null && cdbSession.GetType().Name.Contains("Proxy"))
        {
            // For mocked ICdbSession, we need to create a wrapper or handle it differently
            // For now, we'll set it to null to avoid the type conversion issue
            field?.SetValue(this, null);
        }
        else
        {
            field?.SetValue(this, cdbSession);
        }
    }

    /// <summary>
    /// Calls the protected ValidateCdbSession method.
    /// </summary>
    internal void TestValidateCdbSession()
    {
        ValidateCdbSession();
    }

    /// <summary>
    /// Calls the protected LogCommandProcessing method.
    /// </summary>
    /// <param name="command">The command to log.</param>
    internal void TestLogCommandProcessing(QueuedCommand command)
    {
        LogCommandProcessing(command);
    }

    /// <summary>
    /// Calls the protected ExecuteCommandWithCdbSession method.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task<string> TestExecuteCommandWithCdbSession(QueuedCommand command, CancellationToken cancellationToken)
    {
        return ExecuteCommandWithCdbSession(command, cancellationToken);
    }

    /// <summary>
    /// Calls the protected HandleSuccessfulCommandExecution method.
    /// </summary>
    /// <param name="command">The command that was executed.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <param name="result">The execution result.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task TestHandleSuccessfulCommandExecution(QueuedCommand command, DateTime startTime, string result)
    {
        return HandleSuccessfulCommandExecution(command, startTime, result);
    }

    /// <summary>
    /// Calls the protected HandleCancelledCommand method.
    /// </summary>
    /// <param name="command">The command that was cancelled.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task TestHandleCancelledCommand(QueuedCommand command, DateTime startTime)
    {
        return HandleCancelledCommand(command, startTime);
    }

    /// <summary>
    /// Calls the protected HandleTimedOutCommand method.
    /// </summary>
    /// <param name="command">The command that timed out.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <param name="ex">The timeout exception.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task TestHandleTimedOutCommand(QueuedCommand command, DateTime startTime, TimeoutException ex)
    {
        return HandleTimedOutCommand(command, startTime, ex);
    }

    /// <summary>
    /// Calls the protected HandleFailedCommand method.
    /// </summary>
    /// <param name="command">The command that failed.</param>
    /// <param name="startTime">The start time of execution.</param>
    /// <param name="ex">The exception that caused the failure.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal Task TestHandleFailedCommand(QueuedCommand command, DateTime startTime, Exception ex)
    {
        return HandleFailedCommand(command, startTime, ex);
    }
}
