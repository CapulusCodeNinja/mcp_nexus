using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mcp_nexus.Debugger;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Processes commands with intelligent batching to improve throughput
    /// </summary>
    public class BatchCommandProcessor : IDisposable
    {
        #region Private Fields

        private readonly ICdbSession m_cdbSession;
        private readonly SessionCommandResultCache? m_resultCache;
        private readonly ILogger<BatchCommandProcessor> m_logger;
        private readonly BatchingConfiguration m_config;
        private readonly BatchCommandFilter m_filter;
        private readonly CommandBatchBuilder m_batchBuilder;
        private readonly BatchResultParser m_resultParser;
        private readonly BatchTimeoutCalculator m_timeoutCalculator;

        // Batch management
        private readonly Queue<QueuedCommand> m_batchableCommands;
        private readonly object m_batchLock = new();
        private readonly Timer m_batchTimer;
        private DateTime m_lastBatchTime = DateTime.Now;
        private volatile bool m_disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the BatchCommandProcessor class
        /// </summary>
        /// <param name="cdbSession">The CDB session for executing commands</param>
        /// <param name="resultCache">Optional result cache for storing command results</param>
        /// <param name="logger">The logger instance</param>
        /// <param name="options">The batching configuration options</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        public BatchCommandProcessor(
            ICdbSession cdbSession,
            SessionCommandResultCache? resultCache,
            ILogger<BatchCommandProcessor> logger,
            IOptions<BatchingConfiguration> options)
        {
            m_cdbSession = cdbSession ?? throw new ArgumentNullException(nameof(cdbSession));
            m_resultCache = resultCache;
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (options?.Value == null)
                throw new ArgumentNullException(nameof(options));

            m_config = options.Value;

            // Initialize components
            m_filter = new BatchCommandFilter(options);
            m_batchBuilder = new CommandBatchBuilder();
            m_resultParser = new BatchResultParser();
            m_timeoutCalculator = new BatchTimeoutCalculator(600000, options); // 10 minutes base timeout

            // Initialize batch management
            m_batchableCommands = new Queue<QueuedCommand>();

            // Create timer for batch timeout
            m_batchTimer = new Timer(OnBatchTimeout, null, TimeSpan.FromMilliseconds(m_config.BatchWaitTimeoutMs), TimeSpan.FromMilliseconds(m_config.BatchWaitTimeoutMs));

            m_logger.LogInformation("🚀 BatchCommandProcessor initialized - Enabled: {Enabled}, MaxBatchSize: {MaxBatchSize}, WaitTimeout: {WaitTimeout}ms",
                m_config.Enabled, m_config.MaxBatchSize, m_config.BatchWaitTimeoutMs);
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

            if (m_disposed)
                throw new ObjectDisposedException(nameof(BatchCommandProcessor));

            // If batching is disabled or command cannot be batched, execute immediately
            if (!m_config.Enabled || !m_filter.CanBatchCommand(command.Command ?? string.Empty))
            {
                await ExecuteSingleCommandAsync(command);
                return;
            }

            // Add to batch queue
            lock (m_batchLock)
            {
                m_batchableCommands.Enqueue(command);
                m_logger.LogDebug("📝 Added command {CommandId} to batch queue. Queue size: {QueueSize}",
                    command.Id, m_batchableCommands.Count);

                // Execute batch if we've reached the maximum size
                if (m_batchableCommands.Count >= m_config.MaxBatchSize)
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
                m_logger.LogInformation("⚡ Executing single command {CommandId}: {Command}", command.Id, command.Command);

                var result = await m_cdbSession.ExecuteCommand(command.Command ?? string.Empty, command.CancellationTokenSource?.Token ?? CancellationToken.None);

                var commandResult = CommandResult.Success(result, DateTime.Now - startTime);
                m_resultCache?.StoreResult(command.Id ?? string.Empty, commandResult);

                command.SetResult(result);

                m_logger.LogDebug("✅ Single command {CommandId} completed in {Duration}ms",
                    command.Id, (DateTime.Now - startTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Single command {CommandId} failed: {Error}", command.Id, ex.Message);

                var errorResult = CommandResult.Failure(ex.Message, DateTime.Now - startTime);
                m_resultCache?.StoreResult(command.Id ?? string.Empty, errorResult);

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

            lock (m_batchLock)
            {
                if (m_batchableCommands.Count == 0)
                    return;

                commandsToBatch = m_batchableCommands.ToList();
                m_batchableCommands.Clear();
                m_lastBatchTime = DateTime.Now;
            }

            if (commandsToBatch.Count == 0)
                return;

            var startTime = DateTime.Now;

            try
            {
                m_logger.LogInformation("🔄 Executing batch of {CommandCount} commands", commandsToBatch.Count);

                // Create batch command
                var batchCommand = m_batchBuilder.CreateBatchCommand(commandsToBatch);

                // Calculate timeout
                var batchTimeout = m_timeoutCalculator.CalculateBatchTimeout(commandsToBatch);

                // Execute batch command
                using var timeoutCts = new CancellationTokenSource(batchTimeout);
                var result = await m_cdbSession.ExecuteCommand(batchCommand, timeoutCts.Token);

                // Parse individual results
                var individualResults = m_resultParser.SplitBatchResults(result, commandsToBatch);

                // Store results and complete commands
                for (int i = 0; i < commandsToBatch.Count; i++)
                {
                    var command = commandsToBatch[i];
                    var commandResult = individualResults[i];

                    m_resultCache?.StoreResult(command.Id ?? string.Empty, commandResult);
                    command.SetResult(commandResult.Output);
                }

                var duration = DateTime.Now - startTime;
                m_logger.LogInformation("✅ Batch of {CommandCount} commands completed in {Duration}ms",
                    commandsToBatch.Count, duration.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (startTime.AddMinutes(30) < DateTime.Now)
            {
                m_logger.LogWarning("⏰ Batch command timed out after {Duration}ms", (DateTime.Now - startTime).TotalMilliseconds);

                // Complete all commands with timeout error
                foreach (var command in commandsToBatch)
                {
                    var timeoutResult = CommandResult.Failure($"Batch command timed out after {m_timeoutCalculator.CalculateBatchTimeout(commandsToBatch).TotalMinutes:F1} minutes");
                    m_resultCache?.StoreResult(command.Id ?? string.Empty, timeoutResult);
                    command.SetResult(string.Empty);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Batch execution failed: {Error}", ex.Message);

                // Complete all commands with error
                foreach (var command in commandsToBatch)
                {
                    var errorResult = CommandResult.Failure($"Batch execution failed: {ex.Message}");
                    m_resultCache?.StoreResult(command.Id ?? string.Empty, errorResult);
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
            if (m_disposed)
                return;

            lock (m_batchLock)
            {
                // Execute batch if we've been waiting too long and have commands
                if (m_batchableCommands.Count > 0 &&
                    DateTime.Now - m_lastBatchTime > TimeSpan.FromMilliseconds(m_config.BatchWaitTimeoutMs))
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
            if (m_disposed)
                return;

            m_disposed = true;

            // Execute any remaining commands in the batch
            lock (m_batchLock)
            {
                if (m_batchableCommands.Count > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteBatchAsync();
                        }
                        catch (Exception ex)
                        {
                            m_logger.LogError(ex, "❌ Error executing final batch during disposal");
                        }
                    });
                }
            }

            m_batchTimer?.Dispose();

            m_logger.LogDebug("🏁 BatchCommandProcessor disposed");
        }

        #endregion
    }
}
