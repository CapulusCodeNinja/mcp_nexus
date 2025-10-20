using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Debugger;

namespace mcp_nexus_unit_tests.Mocks
{
    /// <summary>
    /// Realistic mock of ICdbSession that simulates actual CDB behavior
    /// This mock catches bugs that simple string-returning mocks miss
    /// </summary>
    public class RealisticCdbSessionMock : ICdbSession, IDisposable
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, CdbCommandBehavior> _commandBehaviors;
        private readonly Random _random;
        private bool _isActive;
        private bool _disposed;
        private int? _mockProcessId;
        private bool _shouldFailStartSession;
        private bool _shouldFailStopSession;
        private bool _shouldThrowOnCancel;

        public bool IsActive
        {
            get
            {
                if (_disposed) return false;

                // If we have a sequence configured, use it
                if (_isActiveSequence != null && _sequenceIndex < _isActiveSequence.Length)
                {
                    var result = _isActiveSequence[_sequenceIndex];
                    _sequenceIndex++;
                    return result;
                }

                return _isActive;
            }
        }
        public int? ProcessId => _mockProcessId;

        public RealisticCdbSessionMock(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandBehaviors = [];
            _random = new Random();
            _isActive = false;

            // Start session by default to simulate real behavior
            _isActive = true;
            _mockProcessId = _random.Next(1000, 9999);
        }

        public async Task<bool> StartSession(string dumpPath, string? symbolsPath = null)
        {
            if (_disposed) return false;

            _logger.LogInformation("Starting realistic CDB session for dump: {DumpPath}", dumpPath);

            // Simulate session startup delay
            await Task.Delay(100);

            if (_shouldFailStartSession)
            {
                _logger.LogWarning("Simulating StartSession failure");
                return false;
            }

            _isActive = true;
            _mockProcessId = _random.Next(1000, 9999);

            return true;
        }

        public async Task<bool> StopSession()
        {
            if (!_isActive) return false;

            _logger.LogInformation("Stopping realistic CDB session");

            // Simulate session shutdown delay
            await Task.Delay(50);

            if (_shouldFailStopSession)
            {
                _logger.LogWarning("Simulating StopSession failure");
                return false;
            }

            _isActive = false;
            _mockProcessId = null;

            return true;
        }

        public async Task<string> ExecuteCommand(string command)
        {
            return await ExecuteCommand(command, CancellationToken.None);
        }

        public async Task<string> ExecuteCommand(string command, CancellationToken cancellationToken)
        {
            return await ExecuteCommand(command, Guid.NewGuid().ToString(), cancellationToken);
        }

        public async Task<string> ExecuteCommand(string command, string commandId, CancellationToken cancellationToken)
        {
            if (!_isActive)
                throw new InvalidOperationException("Session is not active");

            if (_disposed)
                throw new ObjectDisposedException(nameof(RealisticCdbSessionMock));

            _logger.LogInformation("Executing realistic command: {Command} (ID: {CommandId})", command, commandId);

            // Check for cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            // Find matching behavior or use default
            var behavior = FindMatchingBehavior(command);

            // Simulate command execution delay
            await Task.Delay(behavior.ExecutionDelay, cancellationToken);

            if (behavior.ShouldFail)
            {
                throw new InvalidOperationException($"Command failed: {command}");
            }

            // Simulate realistic output with stderr/stdout coordination
            var result = await SimulateRealisticOutput(behavior, cancellationToken);

            return result;
        }

        public async Task<string> ExecuteBatchCommand(string batchCommand, CancellationToken cancellationToken)
        {
            if (!_isActive)
                throw new InvalidOperationException("Session is not active");

            if (_disposed)
                throw new ObjectDisposedException(nameof(RealisticCdbSessionMock));

            _logger.LogInformation("Executing realistic batch command: {BatchCommand}", batchCommand);

            // Check for cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            // For batch commands, simulate a longer delay and return batch output
            await Task.Delay(100, cancellationToken);

            // Parse the batch command to extract command IDs and generate appropriate output
            var result = "MCP_NEXUS_BATCH_START\n";
            
            // Extract command IDs from the batch command using regex
            var commandIdRegex = new System.Text.RegularExpressions.Regex(@"MCP_NEXUS_CMD_SEP_([^_]+)_START");
            var matches = commandIdRegex.Matches(batchCommand);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var cmdId = match.Groups[1].Value;
                result += $"echo MCP_NEXUS_CMD_SEP_{cmdId}_START\n";
                
                // Generate appropriate output based on command content
                if (batchCommand.Contains("lm"))
                    result += "Module list output\n";
                else if (batchCommand.Contains("!threads"))
                    result += "Thread information\n";
                else if (batchCommand.Contains("!peb"))
                    result += "Process Environment Block\n";
                else
                    result += $"Output for command {cmdId}\n";
                
                result += $"echo MCP_NEXUS_CMD_SEP_{cmdId}_END\n";
            }
            
            result += "MCP_NEXUS_BATCH_END";

            return result;
        }

        public void CancelCurrentOperation()
        {
            _logger.LogInformation("Cancelling current CDB operation");

            if (_shouldThrowOnCancel)
            {
                _logger.LogError("Simulating CancelCurrentOperation failure");
                throw new InvalidOperationException("CDB cancel failed");
            }

            // In a real implementation, this would cancel the current command
        }

        public void AddCommandBehavior(string commandPattern, CdbCommandBehavior behavior)
        {
            _commandBehaviors[commandPattern] = behavior;
        }

        /// <summary>
        /// Configure the mock to simulate different scenarios
        /// </summary>
        public void ConfigureBehavior(bool shouldFailStartSession = false, bool shouldFailStopSession = false, bool shouldThrowOnCancel = false)
        {
            _shouldFailStartSession = shouldFailStartSession;
            _shouldFailStopSession = shouldFailStopSession;
            _shouldThrowOnCancel = shouldThrowOnCancel;
        }

        /// <summary>
        /// Simulate a sequence of IsActive responses (for testing recovery scenarios)
        /// </summary>
        public void SetIsActiveSequence(params bool[] sequence)
        {
            _isActiveSequence = sequence;
            _sequenceIndex = 0;
        }

        private bool[]? _isActiveSequence;
        private int _sequenceIndex = 0;

        private CdbCommandBehavior FindMatchingBehavior(string command)
        {
            // Look for exact match first
            if (_commandBehaviors.TryGetValue(command, out var exactMatch))
                return exactMatch;

            // Look for pattern matches
            foreach (var kvp in _commandBehaviors)
            {
                if (command.Contains(kvp.Key))
                    return kvp.Value;
            }

            // Return default behavior
            return new CdbCommandBehavior
            {
                StdoutLines = ["Mock result"],
                StderrLines = [],
                ExecutionDelay = TimeSpan.FromMilliseconds(10),
                CompletionDelay = TimeSpan.FromMilliseconds(5)
            };
        }

        private static async Task<string> SimulateRealisticOutput(CdbCommandBehavior behavior, CancellationToken cancellationToken)
        {
            var output = new List<string>();

            // Simulate stdout output
            foreach (var line in behavior.StdoutLines)
            {
                output.Add(line);
                await Task.Delay(behavior.CompletionDelay, cancellationToken);
            }

            // Simulate stderr output (this is what catches the bugs!)
            if (behavior.StderrLines.Length > 0)
            {
                output.Add("[STDERR]");
                foreach (var line in behavior.StderrLines)
                {
                    output.Add($"[STDERR] {line}");
                    await Task.Delay(behavior.CompletionDelay, cancellationToken);
                }
            }

            // If no specific behavior was configured, return a generic but realistic output
            if (output.Count == 0)
            {
                output.Add("Mock result");
            }

            return string.Join("\n", output);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _isActive = false;
            _mockProcessId = null;
            _disposed = true;
        }
    }

    /// <summary>
    /// Defines the behavior for a specific CDB command
    /// </summary>
    public class CdbCommandBehavior
    {
        public string[] StdoutLines { get; set; } = [];
        public string[] StderrLines { get; set; } = [];
        public TimeSpan ExecutionDelay { get; set; } = TimeSpan.FromMilliseconds(10);
        public TimeSpan CompletionDelay { get; set; } = TimeSpan.FromMilliseconds(5);
        public bool ShouldFail { get; set; } = false;
    }
}
