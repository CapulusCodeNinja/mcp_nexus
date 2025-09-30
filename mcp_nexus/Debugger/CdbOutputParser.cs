using System.Text.RegularExpressions;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles parsing and analysis of CDB debugger output
    /// </summary>
    public class CdbOutputParser
    {
        private readonly ILogger<CdbOutputParser> m_logger;

        public CdbOutputParser(ILogger<CdbOutputParser> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Determines if a command has completed based on the output line
        /// </summary>
        public bool IsCommandComplete(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            // CDB typically shows "0:000>" prompt when ready for next command
            var hasPrompt = line.Contains(">") && Regex.IsMatch(line, @"\d+:\d+>");

            // Also check for common error patterns that indicate command completion
            var hasError = line.Contains("Unable to") ||
                          line.Contains("Invalid") ||
                          line.Contains("Error") ||
                          line.Contains("syntax error"); // Added for robustness

            var isComplete = hasPrompt || hasError;
            m_logger.LogTrace("IsCommandComplete checking line: '{Line}' -> {IsComplete} (HasPrompt: {HasPrompt}, HasError: {HasError})",
                line, isComplete, hasPrompt, hasError);
            return isComplete;
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
