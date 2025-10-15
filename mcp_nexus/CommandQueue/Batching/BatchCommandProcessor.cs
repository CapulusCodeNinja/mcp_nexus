using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.CommandQueue.Batching
{
    /// <summary>
    /// Processes commands with intelligent batching to improve throughput
    /// </summary>
    public class BatchCommandProcessor : IDisposable
    {
        #region Private Fields

        private readonly ICdbSession m_CdbSession;
        private readonly SessionCommandResultCache? m_ResultCache;
        private readonly ILogger<BatchCommandProcessor> m_Logger;
        private readonly BatchingConfiguration m_Config;
        private readonly string m_SessionId;
        private readonly BatchCommandFilter m_Filter;
        private readonly CommandBatchBuilder m_BatchBuilder;
        private readonly BatchResultParser m_ResultParser;
        private readonly BatchTimeoutCalculator m_TimeoutCalculator;

        // Batch management
        private readonly Queue<QueuedCommand> m_BatchableCommands;
        private readonly object m_BatchLock = new();
        private readonly Timer m_BatchTimer;
        private DateTime m_LastBatchTime = DateTime.Now;
        private volatile bool m_Disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the BatchCommandProcessor class
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands</param>
        /// <param name="resultCache">Optional result cache for storing command results</param>
        /// <param name="logger">The logger instance</param>
        /// <param name="options">The batching configuration options</param>
        /// <param name="sessionId">The session identifier</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        public BatchCommandProcessor(
            ICdbSession cdbSession,
            SessionCommandResultCache? resultCache,
            ILogger<BatchCommandProcessor> logger,
            IOptions<BatchingConfiguration> options,
            string sessionId)
        {
            m_CdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_ResultCache = resultCache;
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));

            if (options?.Value == null)
                throw new ArgumentNullException(nameof(options));

            m_Config = options.Value;

            // Initialize components
            m_Filter = new BatchCommandFilter(options);
            m_BatchBuilder = new CommandBatchBuilder();
            m_ResultParser = new BatchResultParser();
            m_TimeoutCalculator = new BatchTimeoutCalculator(600000, options); // 10 minutes base timeout

            // Initialize batch management
            m_BatchableCommands = new Queue<QueuedCommand>();

            // Create timer for batch timeout (ensure timeout is valid)
            var timeoutMs = Math.Max(1, m_Config.BatchWaitTimeoutMs); // Minimum 1ms to avoid invalid timer values
            m_BatchTimer = new Timer(OnBatchTimeout, null, TimeSpan.FromMilliseconds(timeoutMs), TimeSpan.FromMilliseconds(timeoutMs));

            m_Logger.LogDebug("🚀 BatchCommandProcessor initialized - Enabled: {Enabled}, MaxBatchSize: {MaxBatchSize}, WaitTimeout: {WaitTimeout}ms",
                m_Config.Enabled, m_Config.MaxBatchSize, m_Config.BatchWaitTimeoutMs);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes a command, either by adding it to a batch or executing it immediately
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when command is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the processor has been disposed</exception>
        public async Task ProcessCommandAsync(QueuedCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (m_Disposed)
                throw new ObjectDisposedException(nameof(BatchCommandProcessor));

            // If batching is disabled, command cannot be batched, batch size is invalid, or timeout is invalid, execute immediately
            if (!m_Config.Enabled || !m_Filter.CanBatchCommand(command.Command ?? string.Empty) ||
                m_Config.MaxBatchSize <= 0 || m_Config.BatchWaitTimeoutMs <= 0)
            {
                await ExecuteSingleCommandAsync(command);
                return;
            }

            // Add to batch queue
            lock (m_BatchLock)
            {
                m_BatchableCommands.Enqueue(command);
                m_Logger.LogDebug("📝 Added command {CommandId} to batch queue. Queue size: {QueueSize}",
                    command.Id, m_BatchableCommands.Count);

                // Execute batch if we've reached the maximum size
                if (m_BatchableCommands.Count >= m_Config.MaxBatchSize)
                {
                    _ = Task.Run(ExecuteBatchAsync);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Executes a single command immediately without batching
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ExecuteSingleCommandAsync(QueuedCommand command)
        {
            var startTime = DateTime.Now;

            try
            {
                m_Logger.LogDebug("⚡ Executing single command {CommandId}: {Command}", command.Id, command.Command);

                var result = await m_CdbSession.ExecuteCommand(command.Command ?? string.Empty, command.CancellationTokenSource?.Token ?? CancellationToken.None);

                var commandResult = CommandResult.Success(result, DateTime.Now - startTime);
                m_ResultCache?.StoreResult(command.Id ?? string.Empty, commandResult);

                command.SetResult(result);

                m_Logger.LogDebug("✅ Single command {CommandId} completed in {Duration}ms",
                    command.Id, (DateTime.Now - startTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    m_Logger.LogWarning("❌ Single command {CommandId} cancelled: {Message}", command.Id, ex.Message);
                }
                else
                {
                    m_Logger.LogError(ex, "❌ Single command {CommandId} failed: {Error}", command.Id, ex.Message);
                }

                var errorResult = CommandResult.Failure(ex.Message, DateTime.Now - startTime);
                m_ResultCache?.StoreResult(command.Id ?? string.Empty, errorResult);

                command.SetResult(string.Empty);
            }
        }

        /// <summary>
        /// Executes all commands in the batch queue
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ExecuteBatchAsync()
        {
            List<QueuedCommand> commandsToBatch;

            lock (m_BatchLock)
            {
                if (m_BatchableCommands.Count == 0)
                    return;

                commandsToBatch = m_BatchableCommands.ToList();
                m_BatchableCommands.Clear();
                m_LastBatchTime = DateTime.Now;
            }

            if (commandsToBatch.Count == 0)
                return;

            var startTime = DateTime.Now;

            try
            {
                m_Logger.LogInformation("🔄 Executing batch of {CommandCount} commands", commandsToBatch.Count);

                // Create batch command
                var batchCommand = m_BatchBuilder.CreateBatchCommand(commandsToBatch);

                // Calculate timeout
                var batchTimeout = m_TimeoutCalculator.CalculateBatchTimeout(commandsToBatch);

                // Execute batch command
                using var timeoutCts = new CancellationTokenSource(batchTimeout);
                var result = await m_CdbSession.ExecuteBatchCommand(batchCommand, timeoutCts.Token);

                // Parse individual results
                var individualResults = m_ResultParser.SplitBatchResults(result, commandsToBatch);

                // Store results and complete commands
                for (int i = 0; i < commandsToBatch.Count; i++)
                {
                    var command = commandsToBatch[i];
                    var commandResult = individualResults[i];

                    m_ResultCache?.StoreResult(command.Id ?? string.Empty, commandResult);
                    command.SetResult(commandResult.Output);

                    // INFO: statistical performance log after result is cached for each command in batch
                    var queuedAt = command.QueueTime;
                    var startedAt = startTime; // batch start approximates per-command start within batch
                    var completedAt = DateTime.Now;
                    var timeInQueue = (startedAt - (queuedAt == default ? startedAt : queuedAt)).TotalMilliseconds;
                    var timeExecution = commandResult.Duration.TotalMilliseconds;
                    var totalDuration = (completedAt - (queuedAt == default ? startedAt : queuedAt)).TotalMilliseconds;

                    Utilities.Statistics.CommandStats(
                        m_Logger,
<<<<<<< HEAD
                        Utilities.Statistics.CommandState.SuccessBatch,
                        m_Config.SessionId,
=======
                        "Batch command completed",
                        m_SessionId,
>>>>>>> bf4cc5f (Fix bug in the web config)
                        command.Id,
                        command.Command,
                        queuedAt,
                        startedAt,
                        completedAt,
                        timeInQueue,
                        timeExecution,
                        totalDuration);
                }

                var duration = DateTime.Now - startTime;
                m_Logger.LogInformation("✅ Batch of {CommandCount} commands completed in {Duration}ms",
                    commandsToBatch.Count, duration.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("⏰ Batch command cancelled or timed out after {Duration}ms", (DateTime.Now - startTime).TotalMilliseconds);

                // Complete all commands with timeout error
                foreach (var command in commandsToBatch)
                {
                    var timeoutResult = CommandResult.Failure($"Batch command timed out after {m_TimeoutCalculator.CalculateBatchTimeout(commandsToBatch).TotalMinutes:F1} minutes");
                    m_ResultCache?.StoreResult(command.Id ?? string.Empty, timeoutResult);
                    command.SetResult(string.Empty);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "❌ Batch execution failed: {Error}", ex.Message);

                // Complete all commands with error
                foreach (var command in commandsToBatch)
                {
                    var errorResult = CommandResult.Failure($"Batch execution failed: {ex.Message}");
                    m_ResultCache?.StoreResult(command.Id ?? string.Empty, errorResult);
                    command.SetResult(string.Empty);
                }
            }
        }

        /// <summary>
        /// Timer callback for batch timeout
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void OnBatchTimeout(object? state)
        {
            if (m_Disposed)
                return;

            lock (m_BatchLock)
            {
                // Execute batch if we've been waiting too long and have commands
                if (m_BatchableCommands.Count > 0 &&
                    DateTime.Now - m_LastBatchTime > TimeSpan.FromMilliseconds(m_Config.BatchWaitTimeoutMs))
                {
                    _ = Task.Run(ExecuteBatchAsync);
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the batch command processor
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            // Execute any remaining commands in the batch
            lock (m_BatchLock)
            {
                if (m_BatchableCommands.Count > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteBatchAsync();
                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogError(ex, "❌ Error executing final batch during disposal");
                        }
                    });
                }
            }

            m_BatchTimer?.Dispose();

            m_Logger.LogDebug("🏁 BatchCommandProcessor disposed");
        }

        #endregion
    }
}
