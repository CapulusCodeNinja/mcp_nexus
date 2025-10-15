using System.Text.RegularExpressions;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Contains ultra-safe CDB completion patterns that are guaranteed to be stable across versions.
    /// These patterns are based on binary/structural output formats that Microsoft is unlikely to change.
    /// </summary>
    public static partial class CdbCompletionPatterns
    {
        /// <summary>
        /// Primary CDB prompt pattern - this format has been stable for 20+ years.
        /// Matches patterns like "0:000>", "1:001>", etc.
        /// IMPORTANT: Also matches prompts with output on the same line (e.g., "0:030> command output").
        /// Enhanced to be more robust with whitespace and variations.
        /// </summary>
        public static readonly Regex CdbPromptPattern = MyRegex();

        /// <summary>
        /// CDB prompt at end of line pattern - for mixed output scenarios.
        /// Matches patterns like "\n0:000>" at the end of a line.
        /// Enhanced to be more robust with whitespace variations.
        /// </summary>
        public static readonly Regex CdbPromptEndPattern = new(
            @"(?:^|\r?\n)(\d+):(\d{3})(:[A-Za-z0-9_\-]+)?>\s*$",
            RegexOptions.Compiled);

        /// <summary>
        /// Additional CDB prompt patterns for edge cases and variations.
        /// These patterns catch prompts that might not match the primary patterns.
        /// </summary>
        public static readonly Regex[] AdditionalPromptPatterns = [
            PromptSimple(), // Simple pattern: "0:000>"
            PromptWithText(), // Pattern with trailing content: "0:000> some text"
            PromptEnd(), // Pattern at end of line
        ];

        /// <summary>
        /// Ultra-safe completion patterns that are based on binary/structural formats.
        /// These are unlikely to change across CDB versions or localizations.
        /// NOTE: These should ONLY match command completion, NOT dump file loading output!
        /// </summary>
        public static readonly string[] UltraSafeCompletionPatterns = [
            "^ Syntax error in",    // CDB syntax error format - always starts with ^
            "ModLoad:",             // Module load notification - binary format
            "ModUnload:",           // Module unload notification - binary format
            // REMOVED: "Symbol search path is:" - this appears during dump loading, not just command completion!
            // REMOVED: "Source search path is:" - this appears during dump loading, not just command completion!
        ];

        /// <summary>
        /// Checks if a line contains a CDB prompt pattern.
        /// This method uses both primary and end-of-line prompt patterns for comprehensive detection.
        /// Enhanced with additional patterns for better robustness.
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

            // Check primary patterns first
            if (CdbPromptPattern.IsMatch(trimmedLine) || CdbPromptEndPattern.IsMatch(line))
                return true;

            // Check additional patterns for edge cases
            foreach (var pattern in AdditionalPromptPatterns)
            {
                if (pattern.IsMatch(line))
                    return true;
            }

            return false;
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

        [GeneratedRegex(@"^(\s*)?(\d+):(\d{3})(:[A-Za-z0-9_\-]+)?>\s*", RegexOptions.Multiline | RegexOptions.Compiled)]
        private static partial Regex MyRegex();

        [GeneratedRegex(@"^\s*\d+:\d{3}>\s*$", RegexOptions.Compiled)]
        private static partial Regex PromptSimple();

        [GeneratedRegex(@"^\s*\d+:\d{3}>\s*.*$", RegexOptions.Compiled)]
        private static partial Regex PromptWithText();

        [GeneratedRegex(@"\d+:\d{3}>\s*$", RegexOptions.Compiled)]
        private static partial Regex PromptEnd();
    }
}
