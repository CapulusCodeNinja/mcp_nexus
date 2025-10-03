using System.Text.RegularExpressions;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Contains ultra-safe CDB completion patterns that are guaranteed to be stable across versions.
    /// These patterns are based on binary/structural output formats that Microsoft is unlikely to change.
    /// </summary>
    public static class CdbCompletionPatterns
    {
        /// <summary>
        /// Primary CDB prompt pattern - this format has been stable for 20+ years.
        /// Matches patterns like "0:000>", "1:001>", etc.
        /// IMPORTANT: Also matches prompts with output on the same line (e.g., "0:030> command output").
        /// </summary>
        public static readonly Regex CdbPromptPattern = new Regex(
            @"^(\s*)?(\d+):(\d{3})(:[A-Za-z0-9_\-]+)?>",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// CDB prompt at end of line pattern - for mixed output scenarios.
        /// Matches patterns like "\n0:000>" at the end of a line.
        /// </summary>
        public static readonly Regex CdbPromptEndPattern = new Regex(
            @"(?:^|\r?\n)(\d+):(\d{3})(:[A-Za-z0-9_\-]+)?>\s*$",
            RegexOptions.Compiled);

        /// <summary>
        /// Ultra-safe completion patterns that are based on binary/structural formats.
        /// These are unlikely to change across CDB versions or localizations.
        /// NOTE: These should ONLY match command completion, NOT dump file loading output!
        /// </summary>
        public static readonly string[] UltraSafeCompletionPatterns = {
            "^ Syntax error in",    // CDB syntax error format - always starts with ^
            "ModLoad:",             // Module load notification - binary format
            "ModUnload:",           // Module unload notification - binary format
            // REMOVED: "Symbol search path is:" - this appears during dump loading, not just command completion!
            // REMOVED: "Source search path is:" - this appears during dump loading, not just command completion!
        };

        /// <summary>
        /// Checks if a line contains a CDB prompt pattern.
        /// This method uses both primary and end-of-line prompt patterns for comprehensive detection.
        /// </summary>
        /// <param name="line">The line to check for CDB prompt patterns.</param>
        /// <returns>
        /// <c>true</c> if the line contains a CDB prompt; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsCdbPrompt(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            var trimmedLine = line.Trim();
            return CdbPromptPattern.IsMatch(trimmedLine) || CdbPromptEndPattern.IsMatch(line);
        }

        /// <summary>
        /// Checks if a line contains an ultra-safe completion pattern.
        /// These patterns are guaranteed to be stable and CDB-specific.
        /// </summary>
        /// <param name="line">The line to check for ultra-safe completion patterns.</param>
        /// <returns>
        /// <c>true</c> if the line contains an ultra-safe completion pattern; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsUltraSafeCompletion(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            // Only check patterns that start at the beginning of the line
            // This prevents false positives from quoted text or memory dumps
            return UltraSafeCompletionPatterns.Any(pattern =>
                line.TrimStart().StartsWith(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }
}
