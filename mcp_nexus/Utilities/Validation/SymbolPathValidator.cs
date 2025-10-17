using System.Text.RegularExpressions;

namespace mcp_nexus.Utilities.Validation
{
    /// <summary>
    /// Provides utilities for validating and cleaning symbol search paths to prevent warnings.
    /// Handles whitespace cleanup, path validation, and symbol server configuration.
    /// </summary>
    public static partial class SymbolPathValidator
    {
        /// <summary>
        /// Cleans and validates a symbol search path by removing leading/trailing whitespace and normalizing separators.
        /// </summary>
        /// <param name="symbolPath">The symbol search path to clean.</param>
        /// <returns>A cleaned symbol search path with normalized formatting.</returns>
        /// <exception cref="ArgumentException">Thrown when the symbol path is null or empty after cleaning.</exception>
        public static string CleanSymbolPath(string? symbolPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbolPath))
                    throw new ArgumentException("Symbol path cannot be null or empty", nameof(symbolPath));

                // Split by semicolon and clean each path element
                var pathElements = symbolPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var cleanedElements = new List<string>();

                foreach (var element in pathElements)
                {
                    try
                    {
                        var cleanedElement = CleanPathElement(element);
                        if (!string.IsNullOrWhiteSpace(cleanedElement))
                        {
                            cleanedElements.Add(cleanedElement);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the specific element that failed and continue processing others
                        throw new ArgumentException($"Failed to clean path element '{element}': {ex.Message}", nameof(symbolPath), ex);
                    }
                }

                if (cleanedElements.Count == 0)
                    throw new ArgumentException($"No valid path elements found after cleaning. Original path: '{symbolPath}'", nameof(symbolPath));

                return string.Join(";", cleanedElements);
            }
            catch (ArgumentException)
            {
                // Re-throw ArgumentException as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap unexpected exceptions with context
                throw new ArgumentException($"Unexpected error while cleaning symbol path '{symbolPath}': {ex.Message}", nameof(symbolPath), ex);
            }
        }

        /// <summary>
        /// Cleans a single path element by removing leading/trailing whitespace and normalizing separators.
        /// </summary>
        /// <param name="pathElement">The path element to clean.</param>
        /// <returns>A cleaned path element.</returns>
        /// <exception cref="ArgumentException">Thrown when the path element contains invalid characters or format.</exception>
        private static string CleanPathElement(string pathElement)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pathElement))
                    return string.Empty;

                // Remove leading and trailing whitespace
                var cleaned = pathElement.Trim();

                // Validate path element length
                if (cleaned.Length > 260) // Windows MAX_PATH limit
                    throw new ArgumentException($"Path element too long ({cleaned.Length} characters): '{pathElement}'");

                // Remove any double separators but preserve the original separator type
                if (cleaned.Contains('\\'))
                {
                    // Windows path - normalize backslashes
                    cleaned = MyRegex().Replace(cleaned, "\\");
                }
                else if (cleaned.Contains("://"))
                {
                    // URL - preserve :// and normalize other slashes
                    cleaned = SingleSlashNotUrlRegex().Replace(cleaned, "/");
                }
                else if (cleaned.Contains("cache*") || cleaned.Contains("srv*"))
                {
                    // Symbol path - convert to Windows format with backslashes
                    cleaned = cleaned.Replace('/', '\\');
                    cleaned = MyRegex().Replace(cleaned, "\\");
                }
                else
                {
                    // Regular path - normalize all forward slashes
                    cleaned = MultiSlashRegex().Replace(cleaned, "/");
                }

                return cleaned;
            }
            catch (ArgumentException)
            {
                // Re-throw ArgumentException as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap unexpected exceptions with context
                throw new ArgumentException($"Unexpected error while cleaning path element '{pathElement}': {ex.Message}", nameof(pathElement), ex);
            }
        }

        /// <summary>
        /// Validates that a symbol search path contains valid elements.
        /// </summary>
        /// <param name="symbolPath">The symbol search path to validate.</param>
        /// <returns><c>true</c> if the path is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValidSymbolPath(string? symbolPath)
        {
            if (string.IsNullOrWhiteSpace(symbolPath))
                return false;

            try
            {
                var cleaned = CleanSymbolPath(symbolPath);
                return !string.IsNullOrWhiteSpace(cleaned);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets warnings about potential issues in the symbol search path.
        /// </summary>
        /// <param name="symbolPath">The symbol search path to analyze.</param>
        /// <returns>A list of warning messages about potential issues.</returns>
        public static List<string> GetSymbolPathWarnings(string? symbolPath)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(symbolPath))
            {
                warnings.Add("Symbol path is null or empty");
                return warnings;
            }

            var pathElements = symbolPath.Split(';', StringSplitOptions.None);

            foreach (var element in pathElements)
            {
                var trimmed = element.Trim();

                // Check for leading whitespace
                if (element.StartsWith(' ') || element.StartsWith('\t'))
                {
                    warnings.Add($"Whitespace at start of path element: '{element}'");
                }

                // Check for trailing whitespace
                if (element.EndsWith(' ') || element.EndsWith('\t'))
                {
                    warnings.Add($"Whitespace at end of path element: '{element}'");
                }

                // Check for empty elements
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    warnings.Add("Empty path element found");
                }

                // Check for invalid characters
                if (trimmed.Contains('<') || trimmed.Contains('>') || trimmed.Contains('|'))
                {
                    warnings.Add($"Invalid characters in path element: '{element}'");
                }
            }

            return warnings;
        }

        /// <summary>
        /// Normalizes a symbol search path for consistent formatting.
        /// </summary>
        /// <param name="symbolPath">The symbol search path to normalize.</param>
        /// <returns>A normalized symbol search path.</returns>
        public static string NormalizeSymbolPath(string? symbolPath)
        {
            if (string.IsNullOrWhiteSpace(symbolPath))
                return string.Empty;

            try
            {
                return CleanSymbolPath(symbolPath);
            }
            catch
            {
                return symbolPath ?? string.Empty;
            }
        }

        [GeneratedRegex(@"[\\]+")]
        private static partial Regex MyRegex();

        [GeneratedRegex(@"(?<!:)/(?!/)")]
        private static partial Regex SingleSlashNotUrlRegex();

        [GeneratedRegex(@"[/]+")]
        private static partial Regex MultiSlashRegex();
    }
}
