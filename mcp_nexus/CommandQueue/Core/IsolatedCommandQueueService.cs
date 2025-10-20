using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using mcp_nexus.Debugger;
using mcp_nexus.Notifications;
using mcp_nexus.CommandQueue.Batching;
using mcp_nexus.CommandQueue.Notification;
using mcp_nexus.Utilities;

namespace mcp_nexus.CommandQueue.Core
{
    /// <summary>
    /// Refactored thread-safe, isolated command queue service for a single debugging session
    /// Uses focused components for better maintainability and testability
    /// </summary>
    public class IsolatedCommandQueueService : ICommandQueueService, IDisposable
    {
        private readonly ICdbSession m_CdbSession;
        private readonly ILogger<IsolatedCommandQueueService> m_Logger;
        private readonly IMcpNotificationService m_NotificationService;
        private readonly SessionCommandResultCache m_ResultCache;

        // Focused components
        private readonly CommandQueueConfiguration m_Config;
        private readonly CommandTracker m_Tracker;
        private readonly CommandProcessor m_Processor;
        private readonly CommandNotificationManager m_NotificationManager;
        private readonly BatchingConfiguration? m_BatchingConfig;
        private readonly CommandBatchBuilder m_CommandBatchBuilder;
        private readonly BatchResultParser m_BatchResultParser;

        // Core infrastructure
        private readonly BlockingCollection<QueuedCommand> m_CommandQueue;
        private readonly CancellationTokenSource m_ProcessingCts = new();
        private Task m_ProcessingTask;
        private bool m_Disposed = false;

        // Task recovery infrastructure
        private readonly object m_TaskRestartLock = new();
        private int m_TaskRestartCount = 0;
        private const int MaxTaskRestarts = 3;
        private Exception? m_LastTaskException = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolatedCommandQueueService"/> class.
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands.</param>
        /// <param name="logger">The logger instance for recording queue operations.</param>
        /// <param name="notificationService">The notification service for sending notifications.</param>
        /// <param name="sessionId">The unique identifier for the debugging session.</param>
        /// <param name="loggerFactory">The logger factory for creating additional loggers.</param>
        /// <param name="resultCache">Optional session command result cache for storing results.</param>
        /// <param name="batchingOptions">Optional batching configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public IsolatedCommandQueueService(
            ICdbSession cdbSession,
            ILogger<IsolatedCommandQueueService> logger,
            IMcpNotificationService notificationService,
            string sessionId,
            ILoggerFactory loggerFactory,
            SessionCommandResultCache? resultCache = null,
            IOptions<BatchingConfiguration>? batchingOptions = null)
        {
            m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // Create configuration
            m_Config = new CommandQueueConfiguration(sessionId);

            // Create command queue
            m_CommandQueue = [];

            // Create focused components
            m_Tracker = new CommandTracker(m_Logger, m_Config, m_CommandQueue);
            // Always have a cache to allow post-cleanup retrieval; use provided or create a default
            m_ResultCache = resultCache ?? new SessionCommandResultCache();
            m_Processor = new CommandProcessor(m_CdbSession, m_Logger, m_Config, m_Tracker, m_CommandQueue, m_ProcessingCts, m_ResultCache);
            m_NotificationManager = new CommandNotificationManager(m_NotificationService, m_Logger, m_Config);

            // Store batching configuration for simple batching logic
            m_BatchingConfig = batchingOptions?.Value;
            
            // Initialize batching components
            m_CommandBatchBuilder = new CommandBatchBuilder();
            m_BatchResultParser = new BatchResultParser();

            if (batchingOptions?.Value?.Enabled == true)
            {
                m_Logger.LogInformation("🚀 Simple command batching enabled for session {SessionId} (MinBatchSize: {MinBatchSize}, MaxBatchSize: {MaxBatchSize})",
                    sessionId, batchingOptions.Value.MinBatchSize, batchingOptions.Value.MaxBatchSize);
            }
            else
            {
                m_Logger.LogInformation("⚡ Command batching disabled for session {SessionId}", sessionId);
            }

            m_Logger.LogInformation("🚀 IsolatedCommandQueueService initializing for session {SessionId}", sessionId);

            // Start processing task with proper batching and queue consumption
            m_Logger.LogTrace("🔄 Starting background processing task for session {SessionId}", sessionId);
            m_ProcessingTask = Task.Run(ProcessCommandQueueAsync, m_ProcessingCts.Token);
            m_Logger.LogTrace("✅ Background processing task started for session {SessionId}, Task ID: {TaskId}", sessionId, m_ProcessingTask.Id);

            // Notify startup
            m_NotificationManager.NotifyServiceStartup();

            m_Logger.LogInformation("✅ IsolatedCommandQueueService created for session {SessionId} (background task initializing)", sessionId);
        }

        /// <summary>
        /// Main command processing loop with batching support
        /// </summary>
        private async Task ProcessCommandQueueAsync()
        {
            m_Logger.LogDebug("🔄 Starting command processing loop for session {SessionId}", m_Config.SessionId);

            try
            {
                foreach (var command in m_CommandQueue.GetConsumingEnumerable(m_ProcessingCts.Token))
                {
                    try
                    {
                        m_ProcessingCts.Token.ThrowIfCancellationRequested();

                        var commandsToProcess = new List<QueuedCommand> { command };
                        
                        if (m_BatchingConfig?.Enabled == true)
                        {
                            // Try to collect more commands (non-blocking)
                            var maxAdditional = m_BatchingConfig.MaxBatchSize - 1;
                            for (int i = 0; i < maxAdditional; i++)
                            {
                                if (m_CommandQueue.TryTake(out var additionalCommand, millisecondsTimeout: 0))
                                {
                                    commandsToProcess.Add(additionalCommand);
                                }
                                else break;
                            }
                        }

                        // Decide batching - check if commands can actually be batched
                        bool shouldBatch = m_BatchingConfig?.Enabled == true && 
                                         commandsToProcess.Count >= (m_BatchingConfig.MinBatchSize) &&
                                         commandsToProcess.Count >= 2 &&
                                         CanCommandsBeBatched(commandsToProcess);

                        if (shouldBatch)
                        {
                            m_Logger.LogInformation("📦 Processing batch of {Count} commands", commandsToProcess.Count);
                            
                            var batchCommand = m_CommandBatchBuilder.CreateBatchCommand(commandsToProcess);
                            var batchResult = await m_CdbSession.ExecuteBatchCommand(batchCommand, CancellationToken.None);
                            var results = m_BatchResultParser.SplitBatchResults(batchResult, commandsToProcess);

                            for (int i = 0; i < commandsToProcess.Count; i++)
                            {
                                var cmd = commandsToProcess[i];
                                var commandResult = i < results.Count ? results[i] : null;
                                var resultOutput = commandResult?.Output ?? string.Empty;
                                
                                cmd.State = CommandState.Completed;
                                cmd.CompletionSource?.TrySetResult(resultOutput);
                                m_ResultCache?.StoreResult(cmd.Id ?? string.Empty, CommandResult.Success(resultOutput));
                                
                                // Calculate timing for statistics
                                var now = DateTime.Now;
                                var timeInQueue = (now - cmd.QueueTime).TotalMilliseconds;
                                Statistics.CommandStats(m_Logger, 
                                    Statistics.CommandState.SuccessBatch,
                                    m_Config.SessionId,
                                    cmd.Id ?? string.Empty,
                                    cmd.Command ?? string.Empty,
                                    cmd.QueueTime,
                                    now, // startedAt (approximation)
                                    now, // completedAt 
                                    timeInQueue,
                                    0.0, // executionTime (batch time distributed)
                                    timeInQueue);
                            }
                        }
                        else
                        {
                            // Execute commands individually (single commands or excluded from batching)
                            m_Logger.LogDebug("🎯 Processing {Count} commands individually (single or excluded from batching)", commandsToProcess.Count);
                            foreach (var cmd in commandsToProcess)
                            {
                                m_Tracker.SetCurrentCommand(cmd);
                                await m_Processor.ExecuteCommandSafely(cmd);
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        m_Logger.LogDebug("🛑 Command queue disposed for session {SessionId}", m_Config.SessionId);
                        break;
                    }
                    catch (OperationCanceledException) when (m_ProcessingCts.Token.IsCancellationRequested)
                    {
                        m_Logger.LogDebug("🛑 Command processing cancelled for session {SessionId}", m_Config.SessionId);
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex, "❌ Error in command processing for session {SessionId}", m_Config.SessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Fatal error in command processing for session {SessionId}", m_Config.SessionId);
            }
        }

        /// <summary>
        /// Checks if all commands in the list can be batched (none are excluded)
        /// </summary>
        private bool CanCommandsBeBatched(List<QueuedCommand> commands)
        {
            if (m_BatchingConfig?.ExcludedCommands == null)
                return true;

            foreach (var command in commands)
            {
                var cmd = command.Command?.Trim() ?? string.Empty;
                foreach (var excluded in m_BatchingConfig.ExcludedCommands)
                {
                    if (cmd.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                    {
                        m_Logger.LogDebug("🚫 Command '{Command}' cannot be batched (excluded: {ExcludedPattern})", cmd, excluded);
                        return false; // If ANY command is excluded, don't batch
                    }
                }
            }
            return true; // All commands can be batched
        }

        /// <summary>
        /// Checks if the command queue is ready to accept commands
        /// Attempts automatic recovery if the processing task has faulted
        /// </summary>
        /// <returns>True if ready, false otherwise</returns>
        public bool IsReady()
        {
            if (m_Disposed || m_ProcessingCts.Token.IsCancellationRequested)
                return false;

            if (m_ProcessingTask == null)
                return false;

            // If task is faulted, attempt automatic recovery
            if (m_ProcessingTask.IsFaulted)
            {
                m_Logger.LogWarning("⚠️ Processing task is faulted for session {SessionId}, attempting automatic recovery", m_Config.SessionId);

                try
                {
                    var recovered = RestartProcessingTaskAsync().GetAwaiter().GetResult();
                    if (recovered)
                    {
                        m_Logger.LogInformation("✅ Successfully recovered processing task for session {SessionId}", m_Config.SessionId);
                        return true;
                    }
                    else
                    {
                        m_Logger.LogError("❌ Failed to recover processing task for session {SessionId} (restart limit reached)", m_Config.SessionId);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "❌ Exception during processing task recovery for session {SessionId}", m_Config.SessionId);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to restart the processing task after a fault
        /// </summary>
        /// <returns>True if restart succeeded, false if restart limit reached</returns>
        private Task<bool> RestartProcessingTaskAsync()
        {
            lock (m_TaskRestartLock)
            {
                if (m_TaskRestartCount >= MaxTaskRestarts)
                {
                    m_Logger.LogError("❌ Cannot restart processing task for session {SessionId}: restart limit ({Limit}) reached",
                        m_Config.SessionId, MaxTaskRestarts);
                    return Task.FromResult(false);
                }

                // Log the fault details
                if (m_ProcessingTask.Exception != null)
                {
                    m_LastTaskException = m_ProcessingTask.Exception;
                    m_Logger.LogError(m_ProcessingTask.Exception,
                        "🔥 Processing task faulted for session {SessionId} (restart attempt {Count}/{Max})",
                        m_Config.SessionId, m_TaskRestartCount + 1, MaxTaskRestarts);
                }

                try
                {
                    // Create new processing task (old task is already faulted, no need to wait)
                    m_Logger.LogInformation("🔄 Restarting processing task for session {SessionId} (attempt {Count}/{Max})",
                        m_Config.SessionId, m_TaskRestartCount + 1, MaxTaskRestarts);

                    m_ProcessingTask = Task.Run(() => m_Processor.ProcessCommandQueueAsync(), m_ProcessingCts.Token);
                    m_TaskRestartCount++;

                    m_Logger.LogInformation("✅ Processing task restarted successfully for session {SessionId} (restart count: {Count})",
                        m_Config.SessionId, m_TaskRestartCount);

                    return Task.FromResult(true);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "❌ Failed to restart processing task for session {SessionId}", m_Config.SessionId);
                    return Task.FromResult(false);
                }
            }
        }

        /// <summary>
        /// Queues a command for execution in the isolated queue.
        /// </summary>
        /// <param name="command">The command to queue for execution.</param>
        /// <returns>The unique command ID for tracking the queued command.</returns>
        /// <exception cref="ArgumentException">Thrown when the command is null or empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public string QueueCommand(string command)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            // Generate unique command ID
            var commandId = m_Tracker.GenerateCommandId();

            m_Logger.LogTrace("🔄 Queueing command {CommandId} in session {SessionId}: {Command}",
                commandId, m_Config.SessionId, command);

            // Create command object
            var queuedCommand = new QueuedCommand(
                commandId,
                command,
                DateTime.Now,
                new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously),
                new CancellationTokenSource(),
                CommandState.Queued
            );

            // Add to tracking dictionary first
            m_Logger.LogTrace("🔄 Adding command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_Config.SessionId);
            if (!m_Tracker.TryAddCommand(commandId, queuedCommand))
            {
                queuedCommand.CancellationTokenSource?.Dispose();
                throw new InvalidOperationException($"Command ID conflict: {commandId}");
            }
            m_Logger.LogTrace("✅ Successfully added command {CommandId} to active commands dictionary for session {SessionId}", commandId, m_Config.SessionId);

            try
            {
                // Add to processing queue
                m_Logger.LogTrace("🔄 Adding command {CommandId} to processing queue for session {SessionId}", commandId, m_Config.SessionId);
                m_CommandQueue.Add(queuedCommand, m_ProcessingCts.Token);
                m_Logger.LogTrace("✅ Successfully added command {CommandId} to processing queue for session {SessionId}", commandId, m_Config.SessionId);

                // Notify command queued
                var queuePosition = m_Tracker.GetQueuePosition(commandId);
                var statusMessage = m_NotificationManager.CreateQueuedStatusMessage(queuePosition, TimeSpan.Zero);
                var progress = m_NotificationManager.CalculateQueueProgress(queuePosition, TimeSpan.Zero);

                m_NotificationManager.NotifyCommandStatusFireAndForget(commandId, command, statusMessage, null, progress);

                m_Logger.LogDebug("✅ Command {CommandId} queued successfully for session {SessionId} (position: {Position})",
                    commandId, m_Config.SessionId, queuePosition);

                return commandId;
            }
            catch (Exception ex)
            {
                // Clean up on failure
                m_Tracker.TryRemoveCommand(commandId, out _);
                queuedCommand.CancellationTokenSource?.Dispose();

                m_Logger.LogError(ex, "❌ Failed to queue command {CommandId} for session {SessionId}", commandId, m_Config.SessionId);
                throw;
            }
        }

        /// <summary>
        /// Gets the result of a completed command by its ID.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>
        /// The result of the command if it has completed successfully; otherwise, 
        /// an error message indicating the command status or that it was not found.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public async Task<string> GetCommandResult(string commandId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(commandId))
                return "Command ID cannot be null or empty";

            // First, try to get result from cache (for completed commands that may have been cleaned up)
            ICommandResult? cachedResult = null;

            if (m_BatchingConfig?.Enabled == true)
            {
                // When batching is enabled, results are stored directly in the SessionCommandResultCache
                cachedResult = m_ResultCache?.GetResult(commandId);
            }
            else
            {
                // When batching is disabled, use the traditional CommandProcessor cache
                cachedResult = m_Processor.GetCommandResult(commandId);
            }

            if (cachedResult != null)
            {
                m_Logger.LogDebug("IsolatedCommandQueueService.GetCommandResult: Found in cache - Command {CommandId}, Output length: {Length}, Output: '{Output}', IsSuccess: {IsSuccess}",
                    commandId, cachedResult.Output?.Length ?? 0, cachedResult.Output, cachedResult.IsSuccess);
                return cachedResult.IsSuccess ? (cachedResult.Output ?? string.Empty) : $"Command failed: {cachedResult.ErrorMessage}";
            }

            // If not in cache, check if command is still active in tracker
            var command = m_Tracker.GetCommand(commandId);
            if (command == null)
            {
                m_Logger.LogWarning("Command {CommandId} not found in tracker or cache for session {SessionId}", commandId, m_Config.SessionId);
                return $"Command not found: {commandId}";
            }

            try
            {
                m_Logger.LogTrace("⏳ Waiting for command {CommandId} result in session {SessionId}", commandId, m_Config.SessionId);
                var result = await (command.CompletionSource?.Task ?? Task.FromResult(string.Empty));
                m_Logger.LogTrace("✅ Command {CommandId} result received in session {SessionId}", commandId, m_Config.SessionId);
                return result;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "❌ Error getting result for command {CommandId} in session {SessionId}", commandId, m_Config.SessionId);
                return $"Error getting command result: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the cached result with metadata for a command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>A task that represents the asynchronous operation and contains the cached result with metadata.</returns>
        public async Task<CachedCommandResult?> GetCachedResultWithMetadata(string commandId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(commandId))
                return null;

            try
            {
                // Get the cached result with metadata from the processor
                return await Task.FromResult(m_Processor.GetCachedResultWithMetadata(commandId));
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting cached result with metadata for command {CommandId} in session {SessionId}", commandId, m_Config.SessionId);
                return null;
            }
        }

        public CommandState? GetCommandState(string commandId)
        {
            ThrowIfDisposed();
            return m_Tracker.GetCommandState(commandId);
        }

        public CommandInfo? GetCommandInfo(string commandId)
        {
            ThrowIfDisposed();
            return m_Tracker.GetCommandInfo(commandId);
        }

        /// <summary>
        /// Cancels a queued command by its ID.
        /// </summary>
        /// <param name="commandId">The ID of the command to cancel.</param>
        /// <returns><c>true</c> if the command was found and cancelled; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the command ID is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public bool CancelCommand(string commandId)
        {
            if (m_Disposed)
                return false;

            var success = m_Processor.CancelCommand(commandId);
            if (success)
            {
                m_NotificationManager.NotifyCommandCancellation(commandId, "User requested cancellation");
            }
            return success;
        }

        /// <summary>
        /// Cancels all queued commands.
        /// </summary>
        /// <param name="reason">Optional reason for cancelling all commands.</param>
        /// <returns>The number of commands that were cancelled.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public int CancelAllCommands(string? reason = null)
        {
            if (m_Disposed)
                return 0;

            var count = m_Tracker.CancelAllCommands(reason);
            if (count > 0)
            {
                m_NotificationManager.NotifyBulkCommandCancellation(count, reason ?? "Service shutdown");
            }
            return count;
        }

        public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus()
        {
            if (m_Disposed)
                return [];

            return m_Tracker.GetQueueStatus();
        }

        public QueuedCommand? GetCurrentCommand()
        {
            if (m_Disposed)
                return null;

            return m_Tracker.GetCurrentCommand();
        }

        /// <summary>
        /// Forces immediate shutdown of the command queue service.
        /// This method cancels all pending commands and stops processing immediately.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        public void ForceShutdownImmediate()
        {
            m_Logger.LogWarning("🚨 Force shutdown requested for session {SessionId}", m_Config.SessionId);

            try
            {
                m_ProcessingCts.Cancel();
                // Stop accepting more commands and unblock consumers
                try { m_CommandQueue.CompleteAdding(); } catch { }
                m_NotificationManager.NotifyServiceShutdown("Force shutdown requested");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during force shutdown for session {SessionId}", m_Config.SessionId);
            }
        }

        public (long Total, long Completed, long Failed, long Cancelled) GetPerformanceStats()
        {
            if (m_Disposed)
                return (0, 0, 0, 0);

            return m_Tracker.GetPerformanceStats();
        }

        /// <summary>
        /// Gets diagnostic information about the command queue service
        /// </summary>
        /// <returns>Diagnostic information including task status, restart count, and health metrics</returns>
        public QueueDiagnostics GetDiagnostics()
        {
            return new QueueDiagnostics
            {
                SessionId = m_Config.SessionId,
                IsDisposed = m_Disposed,
                IsCancellationRequested = m_ProcessingCts.Token.IsCancellationRequested,
                TaskStatus = m_ProcessingTask?.Status.ToString() ?? "NotStarted",
                TaskIsFaulted = m_ProcessingTask?.IsFaulted ?? false,
                TaskRestartCount = m_TaskRestartCount,
                MaxTaskRestarts = MaxTaskRestarts,
                LastTaskException = m_LastTaskException?.GetBaseException()?.Message,
                QueueCount = m_CommandQueue?.Count ?? 0,
                IsQueueCompleted = m_CommandQueue?.IsCompleted ?? true,
                PerformanceStats = GetPerformanceStats()
            };
        }

        /// <summary>
        /// Throws an ObjectDisposedException if the service has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the service has been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(IsolatedCommandQueueService));
        }

        /// <summary>
        /// Disposes the isolated command queue service and all its resources.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Logger.LogInformation("🧹 Disposing IsolatedCommandQueueService for session {SessionId}", m_Config.SessionId);

            try
            {
                // Cancel all commands
                var cancelledCount = CancelAllCommands("Service disposal");
                m_Logger.LogInformation("Cancelled {Count} commands during disposal", cancelledCount);

                // Signal shutdown
                m_ProcessingCts.Cancel();
                m_NotificationManager.NotifyServiceShutdown("Service disposed");

                // Complete adding to unblock processing loop immediately
                try { m_CommandQueue.CompleteAdding(); } catch { }

                // Wait for processing to complete
                try
                {
                    if (!m_ProcessingTask.Wait(m_Config.ShutdownTimeout))
                    {
                        m_Logger.LogWarning("Processing task did not complete within {Timeout}ms, forcing shutdown",
                            m_Config.ShutdownTimeout.TotalMilliseconds);
                    }
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                {
                    m_Logger.LogDebug("Processing task was cancelled during shutdown (expected)");
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Error waiting for processing task to complete during disposal");
                }

                // Dispose resources
                m_CommandQueue.Dispose();
                m_ProcessingCts.Dispose();
                m_ResultCache.Dispose();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during disposal of IsolatedCommandQueueService for session {SessionId}", m_Config.SessionId);
            }
            finally
            {
                m_Disposed = true;
                m_Logger.LogInformation("✅ IsolatedCommandQueueService disposed for session {SessionId}", m_Config.SessionId);
            }
        }

        /// <summary>
        /// Gets the cached result of a specific command from the session cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <returns>The cached command result, or null if not found</returns>
        public ICommandResult? GetCachedCommandResult(string commandId)
        {
            return m_Processor.GetCommandResult(commandId);
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        /// <returns>Cache statistics, or null if no cache is available</returns>
        public CacheStatistics? GetCacheStatistics()
        {
            return m_Processor.GetCacheStatistics();
        }

        /// <summary>
        /// Gets all cached command results for completed commands
        /// </summary>
        /// <returns>Dictionary of command ID to cached result for completed commands</returns>
        public Dictionary<string, CachedCommandResult> GetAllCachedResults()
        {
            return m_ResultCache?.GetAllCachedResults() ?? new Dictionary<string, CachedCommandResult>();
        }
    }
}
