using System.Text.RegularExpressions;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles parsing and analysis of CDB debugger output using hybrid approach
    /// Prioritizes ultra-safe patterns over risky string matching to prevent brittleness
    /// </summary>
    public class CdbOutputParser
    {
        private readonly ILogger<CdbOutputParser> m_logger;

        // State tracking for context-aware parsing
        private string? m_currentCommand;
        private readonly List<string> m_outputBuffer = new();

        public CdbOutputParser(ILogger<CdbOutputParser> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets the current command context for stateful parsing
        /// </summary>
        public void SetCurrentCommand(string command)
        {
            m_currentCommand = command?.Trim();
            m_outputBuffer.Clear();
            m_logger.LogTrace("🎯 Set command context: '{Command}'", m_currentCommand);
        }

        /// <summary>
        /// HYBRID: Ultra-safe command completion detection that minimizes brittleness
        /// Uses only the most reliable patterns and lets timeouts handle edge cases
        /// </summary>
        public bool IsCommandComplete(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            AddLineToBuffer(line);

            // 1. PRIMARY: CDB prompts (100% reliable - format stable for 20+ years)
            if (IsCdbPromptDetected(line))
            {
                LogCompletionDetected("CDB prompt", line);
                ResetParserState();
                return true;
            }

            // 2. SECONDARY: Ultra-safe patterns only (binary/structural formats)
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
            m_outputBuffer.Add(line);
        }

        /// <summary>
        /// Detects CDB prompt patterns using ultra-reliable regex
        /// </summary>
        private bool IsCdbPromptDetected(string line)
        {
            return CdbCompletionPatterns.IsCdbPrompt(line);
        }

        /// <summary>
        /// Detects ultra-safe completion patterns that are guaranteed stable
        /// </summary>
        private bool IsUltraSafeCompletionDetected(string line)
        {
            return CdbCompletionPatterns.IsUltraSafeCompletion(line);
        }

        /// <summary>
        /// Logs when command completion is detected
        /// </summary>
        private void LogCompletionDetected(string detectionType, string line)
        {
            m_logger.LogTrace("✅ COMPLETION detected via {DetectionType}: '{Line}'", detectionType, line);
        }

        /// <summary>
        /// Logs when command is still executing
        /// </summary>
        private void LogCommandStillExecuting(string line)
        {
            m_logger.LogTrace("⏳ Command still executing: '{Line}' (Buffer: {BufferSize} lines)",
                line, m_outputBuffer.Count);
        }

        /// <summary>
        /// Resets parser state after command completion
        /// </summary>
        private void ResetParserState()
        {
            m_currentCommand = null;
            m_outputBuffer.Clear();
        }

        /// <summary>
        /// Captures any available output from the debugger streams
        /// </summary>
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
            var lines = new List<string>();

            try
            {
                // Read all immediately available lines
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        lines.Add(line);
                    }
                    else
                    {
                        break; // No more data available
                    }
                }
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error reading available lines from {StreamName}", streamName);
            }

            return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : string.Empty;
        }

        /// <summary>
        /// Formats output for logging with proper truncation and sanitization
        /// </summary>
        public string FormatOutputForLogging(string output, int maxLength = 1000)
        {
            if (string.IsNullOrEmpty(output))
                return "[Empty]";

            // Remove sensitive information or control characters if needed
            var sanitized = output.Replace("\0", "\\0"); // Replace null characters

            if (sanitized.Length <= maxLength)
                return sanitized;

            return sanitized.Substring(0, maxLength) + "... [truncated]";
        }

        /// <summary>
        /// Analyzes output for common error patterns and warnings
        /// </summary>
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
            analysis.HasPrompt = Regex.IsMatch(output, @"\d+:\d+>");

            return analysis;
        }
    }

    /// <summary>
    /// Results of output analysis
    /// </summary>
    public class OutputAnalysis
    {
        public bool IsEmpty { get; set; }
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public bool HasSuccessIndicators { get; set; }
        public bool HasPrompt { get; set; }
    }
}
