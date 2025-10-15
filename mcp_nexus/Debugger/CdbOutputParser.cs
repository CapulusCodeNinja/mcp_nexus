using System.Text.RegularExpressions;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles parsing and analysis of CDB debugger output using hybrid approach.
    /// Prioritizes ultra-safe patterns over risky string matching to prevent brittleness.
    /// Provides command completion detection, output analysis, and context-aware parsing.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CdbOutputParser"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance for recording parsing operations and errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public partial class CdbOutputParser(ILogger<CdbOutputParser> logger)
    {
        private readonly ILogger<CdbOutputParser> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // State tracking for context-aware parsing
        private string? m_CurrentCommand;
        private readonly List<string> m_OutputBuffer = [];

        /// <summary>
        /// Sets the current command context for stateful parsing.
        /// This method clears the output buffer and sets the command context for better completion detection.
        /// </summary>
        /// <param name="command">The command being executed. Can be null.</param>
        public void SetCurrentCommand(string command)
        {
            m_CurrentCommand = command?.Trim();
            m_OutputBuffer.Clear();
            m_Logger.LogTrace("🎯 Set command context: '{Command}'", m_CurrentCommand);
        }

        /// <summary>
        /// Determines if a command has completed execution using ultra-safe detection patterns.
        /// This method uses a hybrid approach that prioritizes reliable patterns over risky string matching.
        /// Enhanced with detailed debug logging for troubleshooting.
        /// </summary>
        /// <param name="line">The current line of output from the CDB process.</param>
        /// <returns>
        /// <c>true</c> if the command has completed; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCommandComplete(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            AddLineToBuffer(line);

            // 1. PRIMARY: Check for sentinel markers first (highest priority)
            if (IsSentinelDetected(line))
            {
                LogCompletionDetected("Sentinel marker", line);
                ResetParserState();
                return true;
            }

            // 2. SECONDARY: CDB prompts (100% reliable - format stable for 20+ years)
            if (IsCdbPromptDetected(line))
            {
                LogCompletionDetected("CDB prompt", line);
                ResetParserState();
                return true;
            }

            // 3. TERTIARY: Ultra-safe patterns only (binary/structural formats)
            if (IsUltraSafeCompletionDetected(line))
            {
                LogCompletionDetected("Ultra-safe pattern", line);
                ResetParserState();
                return true;
            }

            // 3. FALLBACK: Let timeouts handle everything else to avoid brittleness
            LogCommandStillExecuting(line);
            return false;
        }

        /// <summary>
        /// Adds a line to the output buffer for context tracking
        /// </summary>
        private void AddLineToBuffer(string line)
        {
            m_OutputBuffer.Add(line);
        }

        /// <summary>
        /// Detects sentinel markers for command completion
        /// </summary>
        private static bool IsSentinelDetected(string line)
        {
            // Check for start sentinel
            if (line.Contains(CdbSentinels.StartMarker))
            {
                return false; // Don't complete on start sentinel, wait for end sentinel
            }

            // Check for end sentinel
            if (line.Contains(CdbSentinels.EndMarker))
            {
                return true; // Complete when end sentinel is found
            }

            return false;
        }

        /// <summary>
        /// Detects CDB prompt patterns using ultra-reliable regex
        /// </summary>
        private static bool IsCdbPromptDetected(string line)
        {
            return CdbCompletionPatterns.IsCdbPrompt(line);
        }

        /// <summary>
        /// Detects ultra-safe completion patterns that are guaranteed stable
        /// </summary>
        private static bool IsUltraSafeCompletionDetected(string line)
        {
            return CdbCompletionPatterns.IsUltraSafeCompletion(line);
        }

        /// <summary>
        /// Logs when command completion is detected
        /// </summary>
        private void LogCompletionDetected(string detectionType, string line)
        {
            m_Logger.LogTrace("✅ COMPLETION detected via {DetectionType}: '{Line}'", detectionType, line);
        }

        /// <summary>
        /// Logs when command is still executing
        /// </summary>
        private void LogCommandStillExecuting(string line)
        {
            m_Logger.LogTrace("⏳ Command still executing: '{Line}' (Buffer: {BufferSize} lines)",
                line, m_OutputBuffer.Count);
        }

        /// <summary>
        /// Resets parser state after command completion
        /// </summary>
        private void ResetParserState()
        {
            m_CurrentCommand = null;
            m_OutputBuffer.Clear();
        }

        /// <summary>
        /// Captures any available output from the debugger streams without blocking.
        /// This method reads all immediately available lines from both stdout and stderr streams.
        /// </summary>
        /// <param name="outputReader">The stdout stream reader. Can be null.</param>
        /// <param name="errorReader">The stderr stream reader. Can be null.</param>
        /// <param name="context">The context for logging purposes.</param>
        /// <param name="logger">The logger instance for recording captured output.</param>
        public void CaptureAvailableOutput(StreamReader? outputReader, StreamReader? errorReader, string context, ILogger logger)
        {
            try
            {
                logger.LogTrace("Capturing available output for context: {Context}", context);

                // Capture stdout
                if (outputReader != null && !outputReader.EndOfStream)
                {
                    var availableOutput = ReadAvailableLines(outputReader, "stdout");
                    if (!string.IsNullOrEmpty(availableOutput))
                    {
                        logger.LogDebug("[{Context}] Available stdout: {Output}", context, availableOutput);
                    }
                }

                // Capture stderr
                if (errorReader != null && !errorReader.EndOfStream)
                {
                    var availableError = ReadAvailableLines(errorReader, "stderr");
                    if (!string.IsNullOrEmpty(availableError))
                    {
                        logger.LogDebug("[{Context}] Available stderr: {Error}", context, availableError);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error capturing available output for context: {Context}", context);
            }
        }

        /// <summary>
        /// Reads all available lines from a stream reader without blocking
        /// </summary>
        private string ReadAvailableLines(StreamReader reader, string streamName)
        {
            var sb = new System.Text.StringBuilder();

            try
            {
                // Read all immediately available lines
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break; // No more data available
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(Environment.NewLine);
                    }
                    sb.Append(line);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error reading available lines from {StreamName}", streamName);
            }

            return sb.Length > 0 ? sb.ToString() : string.Empty;
        }

        /// <summary>
        /// Formats output for logging with proper truncation and sanitization.
        /// This method removes control characters and truncates long output for safe logging.
        /// </summary>
        /// <param name="output">The output string to format.</param>
        /// <param name="maxLength">The maximum length before truncation. Default is 1000 characters.</param>
        /// <returns>
        /// The formatted output string, truncated if necessary and with control characters sanitized.
        /// </returns>
        public string FormatOutputForLogging(string output, int maxLength = 1000)
        {
            if (string.IsNullOrEmpty(output))
                return "[Empty]";

            // Remove sensitive information or control characters if needed
            var sanitized = output.Replace("\0", "\\0"); // Replace null characters

            if (sanitized.Length <= maxLength)
                return sanitized;

            return sanitized[..maxLength] + "... [truncated]";
        }

        /// <summary>
        /// Analyzes output for common error patterns, warnings, and success indicators.
        /// This method performs pattern matching to identify the nature of the output.
        /// </summary>
        /// <param name="output">The output string to analyze.</param>
        /// <returns>
        /// An <see cref="OutputAnalysis"/> object containing the analysis results.
        /// </returns>
        public OutputAnalysis AnalyzeOutput(string output)
        {
            var analysis = new OutputAnalysis();

            if (string.IsNullOrEmpty(output))
            {
                analysis.IsEmpty = true;
                return analysis;
            }

            // Check for error patterns
            analysis.HasErrors = output.Contains("Error") ||
                               output.Contains("Unable to") ||
                               output.Contains("Invalid") ||
                               output.Contains("Failed");

            // Check for warnings
            analysis.HasWarnings = output.Contains("Warning") ||
                                 output.Contains("WARN") ||
                                 output.Contains("Caution");

            // Check for success indicators
            analysis.HasSuccessIndicators = output.Contains("Success") ||
                                          output.Contains("OK") ||
                                          output.Contains("Complete");

            // Check for prompts
            analysis.HasPrompt = MyRegex().IsMatch(output);

            return analysis;
        }

        [GeneratedRegex(@"\d+:\d+>")]
        private static partial Regex MyRegex();
    }

    /// <summary>
    /// Results of output analysis containing various indicators about the analyzed output.
    /// </summary>
    public class OutputAnalysis
    {
        /// <summary>
        /// Gets or sets a value indicating whether the output is empty.
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the output contains error patterns.
        /// </summary>
        public bool HasErrors { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the output contains warning patterns.
        /// </summary>
        public bool HasWarnings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the output contains success indicators.
        /// </summary>
        public bool HasSuccessIndicators { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the output contains CDB prompt patterns.
        /// </summary>
        public bool HasPrompt { get; set; }
    }
}
