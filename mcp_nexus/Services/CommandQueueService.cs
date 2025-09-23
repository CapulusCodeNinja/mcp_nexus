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
            
            // Start the background processing task
            m_processingTask = Task.Run(ProcessCommandQueue, m_serviceCts.Token);
            m_logger.LogInformation("CommandQueueService started with background processing");
        }

        public string QueueCommand(string command)
        {
            var commandId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();
            var cts = new CancellationTokenSource();
            
            var queuedCommand = new QueuedCommand(commandId, command, DateTime.UtcNow, tcs, cts);
            
            m_activeCommands[commandId] = queuedCommand;
            m_commandQueue.Enqueue(queuedCommand);
            m_queueSemaphore.Release(); // Signal that a command is available
            
            m_logger.LogInformation("Queued command {CommandId}: {Command} (Queue size: {QueueSize})", 
                commandId, command, m_commandQueue.Count);
            
            return commandId;
        }

        public Task<string> GetCommandResult(string commandId)
        {
            if (m_activeCommands.TryGetValue(commandId, out var command))
            {
                return command.CompletionSource.Task;
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
                if (cmd.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    results.Add((cmd.Id, cmd.Command, cmd.QueueTime, "Cancelled"));
                }
                else
                {
                    results.Add((cmd.Id, cmd.Command, cmd.QueueTime, "Queued"));
                }
            }
            
            return results;
        }

        private async Task ProcessCommandQueue()
        {
            m_logger.LogInformation("Command queue processing started");
            
            try
            {
                while (!m_serviceCts.Token.IsCancellationRequested)
                {
                    // Wait for a command to be available
                    await m_queueSemaphore.WaitAsync(m_serviceCts.Token);
                    
                    if (m_commandQueue.TryDequeue(out var queuedCommand))
                    {
                        // Check if command was cancelled while queued
                        if (queuedCommand.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            m_logger.LogInformation("Skipping cancelled command {CommandId}: {Command}", 
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
                        
                        m_logger.LogInformation("Executing command {CommandId}: {Command} (waited {WaitTime:F1}s in queue)", 
                            queuedCommand.Id, queuedCommand.Command, 
                            (DateTime.UtcNow - queuedCommand.QueueTime).TotalSeconds);
                        
                        try
                        {
                            // Execute the command
                            var result = await m_cdbSession.ExecuteCommand(queuedCommand.Command, queuedCommand.CancellationTokenSource.Token);
                            
                            if (queuedCommand.CancellationTokenSource.Token.IsCancellationRequested)
                            {
                                queuedCommand.CompletionSource.SetResult("Command execution was cancelled.");
                            }
                            else
                            {
                                queuedCommand.CompletionSource.SetResult(result);
                            }
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
            m_activeCommands.TryRemove(command.Id, out _);
            command.CancellationTokenSource.Dispose();
            
            m_logger.LogDebug("Cleaned up command {CommandId}", command.Id);
        }

        public void Dispose()
        {
            m_logger.LogInformation("Shutting down CommandQueueService");
            
            m_serviceCts.Cancel();
            
            try
            {
                m_processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error waiting for processing task to complete");
            }
            
            // Cancel all pending commands
            foreach (var command in m_activeCommands.Values)
            {
                command.CancellationTokenSource.Cancel();
                command.CompletionSource.TrySetResult("Service is shutting down.");
                command.CancellationTokenSource.Dispose();
            }
            
            m_activeCommands.Clear();
            m_serviceCts.Dispose();
            m_queueSemaphore.Dispose();
            
            m_logger.LogInformation("CommandQueueService disposed");
        }
    }
}
