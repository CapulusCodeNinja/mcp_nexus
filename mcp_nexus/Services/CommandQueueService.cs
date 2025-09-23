using System.Collections.Concurrent;
using mcp_nexus.Helper;

namespace mcp_nexus.Services
{
    public record QueuedCommand(
        string Id,
        string Command,
        DateTime QueueTime,
        TaskCompletionSource<string> CompletionSource,
        CancellationTokenSource CancellationTokenSource
    );

    public class CommandQueueService : IDisposable
    {
        private readonly CdbSession m_cdbSession;
        private readonly ILogger<CommandQueueService> m_logger;
        private readonly ConcurrentQueue<QueuedCommand> m_commandQueue = new();
        private readonly SemaphoreSlim m_queueSemaphore = new(0);
        private readonly CancellationTokenSource m_serviceCts = new();
        private readonly Task m_processingTask;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_activeCommands = new();
        private QueuedCommand? m_currentCommand;
        private readonly object m_currentCommandLock = new();

        public CommandQueueService(CdbSession cdbSession, ILogger<CommandQueueService> logger)
        {
            m_cdbSession = cdbSession;
            m_logger = logger;

            m_logger.LogInformation("🚀 CommandQueueService CONSTRUCTOR started");

            // Start the background processing task
            try
            {
                m_processingTask = Task.Run(ProcessCommandQueue, m_serviceCts.Token);
                m_logger.LogInformation("✅ CommandQueueService background task started successfully");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ FAILED to start CommandQueueService background task");
                throw;
            }

            m_logger.LogInformation("🎯 CommandQueueService fully initialized");
        }

        public string QueueCommand(string command)
        {
            var commandId = Guid.NewGuid().ToString();
            m_logger.LogInformation("🔄 QueueCommand START: {CommandId} for command: {Command}", commandId, command);

            var tcs = new TaskCompletionSource<string>();
            var cts = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, DateTime.UtcNow, tcs, cts);

            m_logger.LogInformation("📝 Adding to activeCommands dictionary: {CommandId}", commandId);
            m_activeCommands[commandId] = queuedCommand;

            m_logger.LogInformation("📋 Enqueueing command: {CommandId}", commandId);
            m_commandQueue.Enqueue(queuedCommand);

            m_logger.LogInformation("🔔 Releasing semaphore for command: {CommandId}", commandId);
            m_queueSemaphore.Release(); // Signal that a command is available

            m_logger.LogInformation("✅ QueueCommand COMPLETE: {CommandId} (Queue size: {QueueSize})",
                commandId, m_commandQueue.Count);

            return commandId;
        }

        public Task<string> GetCommandResult(string commandId)
        {
            // NOTE: Completed commands stay in m_activeCommands for result retrieval
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                // DON'T WAIT! Check if completed, return status immediately
                if (command.CompletionSource.Task.IsCompleted)
                {
                    try
                    {
                        var result = command.CompletionSource.Task.Result;
                        return Task.FromResult(result);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult($"Command failed: {ex.Message}");
                    }
                }
                else
                {
                    // Command still running - return status immediately, don't wait!
                    return Task.FromResult($"Command is still executing... Please call get_command_status(commandId='{commandId}') again in 5-10 seconds to check if completed.");
                }
            }

            return Task.FromResult($"Command not found: {commandId}");
        }

        public bool CancelCommand(string commandId)
        {
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                m_logger.LogInformation("Cancelling command {CommandId}: {Command}", commandId, command.Command);
                command.CancellationTokenSource.Cancel();

                // If this is the currently executing command, also cancel the CDB operation
                lock (m_currentCommandLock)
                {
                    if (m_currentCommand?.Id == commandId)
                    {
                        m_logger.LogWarning("Cancelling currently executing command {CommandId}", commandId);
                        m_cdbSession.CancelCurrentOperation();
                    }
                }

                return true;
            }

            m_logger.LogWarning("Attempted to cancel non-existent command: {CommandId}", commandId);
            return false;
        }

        public QueuedCommand? GetCurrentCommand()
        {
            lock (m_currentCommandLock)
            {
                return m_currentCommand;
            }
        }

        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            var results = new List<(string, string, DateTime, string)>();

            // Add current command
            lock (m_currentCommandLock)
            {
                if (m_currentCommand != null)
                {
                    results.Add((m_currentCommand.Id, m_currentCommand.Command, m_currentCommand.QueueTime, "Executing"));
                }
            }

            // Add queued commands
            foreach (var cmd in m_commandQueue)
            {
                results.Add(cmd.CancellationTokenSource.Token.IsCancellationRequested
                    ? (cmd.Id, cmd.Command, cmd.QueueTime, "Cancelled")
                    : (cmd.Id, cmd.Command, cmd.QueueTime, "Queued"));
            }

            return results;
        }

        private async Task ProcessCommandQueue()
        {
            m_logger.LogInformation("🔥 BACKGROUND PROCESSOR: ProcessCommandQueue started");

            try
            {
                while (!m_serviceCts.Token.IsCancellationRequested)
                {
                    m_logger.LogInformation("⏳ BACKGROUND PROCESSOR: Waiting for command (semaphore)...");

                    // Wait for a command to be available
                    await m_queueSemaphore.WaitAsync(m_serviceCts.Token);

                    m_logger.LogInformation("🔔 BACKGROUND PROCESSOR: Semaphore released, checking queue...");

                    if (m_commandQueue.TryDequeue(out var queuedCommand))
                    {
                        m_logger.LogInformation("📦 BACKGROUND PROCESSOR: Dequeued command {CommandId}: {Command}",
                            queuedCommand.Id, queuedCommand.Command);

                        // Check if command was cancelled while queued
                        if (queuedCommand.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            m_logger.LogInformation("❌ BACKGROUND PROCESSOR: Skipping cancelled command {CommandId}: {Command}",
                                queuedCommand.Id, queuedCommand.Command);

                            queuedCommand.CompletionSource.SetResult("Command was cancelled while queued.");
                            CleanupCommand(queuedCommand);
                            continue;
                        }

                        // Set as current command
                        lock (m_currentCommandLock)
                        {
                            m_currentCommand = queuedCommand;
                        }

                        var waitTime = (DateTime.UtcNow - queuedCommand.QueueTime).TotalSeconds;
                        m_logger.LogInformation("🚀 BACKGROUND PROCESSOR: Starting execution of {CommandId}: {Command} (waited {WaitTime:F1}s in queue)",
                            queuedCommand.Id, queuedCommand.Command, waitTime);

                        m_logger.LogInformation("🔧 BACKGROUND PROCESSOR: Checking CDB session status...");
                        m_logger.LogInformation("🔧 BACKGROUND PROCESSOR: CdbSession.IsActive = {IsActive}", m_cdbSession.IsActive);

                        try
                        {
                            m_logger.LogInformation("⚡ BACKGROUND PROCESSOR: Calling CdbSession.ExecuteCommand for {CommandId}", queuedCommand.Id);

                            // Execute the command
                            var result = await m_cdbSession.ExecuteCommand(queuedCommand.Command, queuedCommand.CancellationTokenSource.Token);

                            m_logger.LogInformation("✅ BACKGROUND PROCESSOR: CdbSession.ExecuteCommand completed for {CommandId}", queuedCommand.Id);

                            queuedCommand.CompletionSource.SetResult(
                                queuedCommand.CancellationTokenSource.Token.IsCancellationRequested
                                    ? "Command execution was cancelled."
                                    : result);
                        }
                        catch (OperationCanceledException)
                        {
                            m_logger.LogInformation("Command {CommandId} was cancelled during execution", queuedCommand.Id);
                            queuedCommand.CompletionSource.SetResult("Command execution was cancelled.");
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogError(ex, "Error executing command {CommandId}: {Command}", queuedCommand.Id, queuedCommand.Command);
                            queuedCommand.CompletionSource.SetResult($"Command execution failed: {ex.Message}");
                        }
                        finally
                        {
                            // Clear current command
                            lock (m_currentCommandLock)
                            {
                                m_currentCommand = null;
                            }

                            CleanupCommand(queuedCommand);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                m_logger.LogInformation("Command queue processing was cancelled");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Unexpected error in command queue processing");
            }
        }

        private void CleanupCommand(QueuedCommand command)
        {
            // DON'T remove from m_activeCommands - let completed commands stay for retrieval!
            // Only dispose the cancellation token to free resources
            command.CancellationTokenSource.Dispose();

            m_logger.LogDebug("Cleaned up command resources for {CommandId} (kept in activeCommands for result retrieval)", command.Id);
        }

        public void Dispose()
        {
            m_logger.LogInformation("Shutting down CommandQueueService");

            // Check if already disposed
            if (m_serviceCts.Token.IsCancellationRequested)
            {
                m_logger.LogWarning("CommandQueueService already disposed");
                return;
            }

            try
            {
                m_serviceCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                m_logger.LogWarning("CancellationTokenSource already disposed during shutdown");
                return;
            }

            try
            {
                m_processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error waiting for processing task to complete");
            }

            // Cancel all pending commands with disposal guards
            foreach (var command in m_activeCommands.Values)
            {
                try
                {
                    if (!command.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        command.CancellationTokenSource.Cancel();
                    }
                    command.CompletionSource.TrySetResult("Service is shutting down.");
                }
                catch (ObjectDisposedException)
                {
                    // CancellationTokenSource already disposed, ignore
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error cancelling command {CommandId}", command.Id);
                }

                try
                {
                    command.CancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
            }

            m_activeCommands.Clear();

            try
            {
                m_serviceCts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            try
            {
                m_queueSemaphore.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            m_logger.LogInformation("CommandQueueService disposed");
        }
    }
}
