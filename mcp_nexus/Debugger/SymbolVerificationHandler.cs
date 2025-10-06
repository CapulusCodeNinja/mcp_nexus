using System.Text.RegularExpressions;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Handles symbol verification warnings and provides enhanced symbol loading capabilities.
    /// Manages checksum verification warnings and provides fallback mechanisms for third-party software.
    /// </summary>
    public class SymbolVerificationHandler
    {
        private readonly ILogger<SymbolVerificationHandler> m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolVerificationHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for this handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public SymbolVerificationHandler(ILogger<SymbolVerificationHandler> logger)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes symbol verification warnings and provides appropriate handling.
        /// </summary>
        /// <param name="output">The CDB output containing symbol verification information.</param>
        /// <returns>A processed output with warnings handled appropriately.</returns>
        public string ProcessSymbolWarnings(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return output;

            var processedOutput = output;
            var warnings = ExtractSymbolWarnings(output);

            foreach (var warning in warnings)
            {
                var handledWarning = HandleSymbolWarning(warning);
                if (handledWarning != null)
                {
                    processedOutput = processedOutput.Replace(warning, handledWarning);
                }
            }

            return processedOutput;
        }

        /// <summary>
        /// Extracts symbol verification warnings from CDB output.
        /// </summary>
        /// <param name="output">The CDB output to analyze.</param>
        /// <returns>A list of symbol verification warnings found in the output.</returns>
        public List<string> ExtractSymbolWarnings(string output)
        {
            var warnings = new List<string>();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check for checksum verification warnings
                if (trimmedLine.Contains("WARNING: Unable to verify checksum"))
                {
                    warnings.Add(trimmedLine);
                }
                // Check for symbol loading warnings
                else if (trimmedLine.Contains("WARNING:") && trimmedLine.Contains("symbol"))
                {
                    warnings.Add(trimmedLine);
                }
                // Check for module verification warnings
                else if (trimmedLine.Contains("WARNING:") && trimmedLine.Contains("module"))
                {
                    warnings.Add(trimmedLine);
                }
            }

            return warnings;
        }

        /// <summary>
        /// Handles a specific symbol verification warning.
        /// </summary>
        /// <param name="warning">The warning message to handle.</param>
        /// <returns>A processed warning message or null if the warning should be suppressed.</returns>
        private string? HandleSymbolWarning(string warning)
        {
            if (string.IsNullOrWhiteSpace(warning))
                return null;

            // Extract module name from warning
            var moduleName = ExtractModuleNameFromWarning(warning);

            if (string.IsNullOrWhiteSpace(moduleName))
                return warning; // Return original warning if we can't extract module name

            // Check if this is a third-party software module
            if (IsThirdPartyModule(moduleName))
            {
                m_Logger.LogInformation("🔍 Third-party module symbol warning handled: {ModuleName} - {Warning}",
                    moduleName, warning);

                // For third-party software, we can provide a more informative message
                return $"INFO: Symbol verification warning for third-party module '{moduleName}' - this is normal for non-Microsoft software";
            }

            // For Microsoft modules, keep the original warning but add context
            m_Logger.LogWarning("⚠️ Microsoft module symbol warning: {ModuleName} - {Warning}",
                moduleName, warning);

            return $"WARNING: {warning} - Consider updating symbols or checking module integrity";
        }

        /// <summary>
        /// Extracts the module name from a symbol verification warning.
        /// </summary>
        /// <param name="warning">The warning message to analyze.</param>
        /// <returns>The module name if found; otherwise, null.</returns>
        private string? ExtractModuleNameFromWarning(string warning)
        {
            // Pattern to match module names in warnings like "WARNING: Unable to verify checksum for moduleName.dll"
            var pattern = @"WARNING:.*?for\s+([a-zA-Z0-9_.-]+\.(dll|exe|sys))";
            var match = Regex.Match(warning, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups[1].Value;

            return null;
        }

        /// <summary>
        /// Determines if a module is from third-party software.
        /// </summary>
        /// <param name="moduleName">The module name to check.</param>
        /// <returns><c>true</c> if the module is from third-party software; otherwise, <c>false</c>.</returns>
        private bool IsThirdPartyModule(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                return false;

            var normalizedName = moduleName.ToLowerInvariant();

            // Common third-party software modules
            var thirdPartyModules = new[]
            {
                "avast", "norton", "mcafee", "kaspersky", "bitdefender", "eset", "trend",
                "chrome", "firefox", "opera", "edge", "safari",
                "adobe", "acrobat", "reader", "flash", "shockwave",
                "java", "jre", "jdk", "oracle",
                "vmware", "virtualbox", "hyper-v",
                "nvidia", "amd", "intel", "realtek", "creative",
                "steam", "origin", "epic", "uplay",
                "office", "word", "excel", "powerpoint", "outlook",
                "skype", "teams", "zoom", "discord", "slack",
                "winrar", "7zip", "winzip", "peazip",
                "vlc", "media", "player", "codec",
                "python", "node", "npm", "git", "svn"
            };

            return thirdPartyModules.Any(module => normalizedName.Contains(module));
        }

        /// <summary>
        /// Gets symbol loading recommendations based on warnings found.
        /// </summary>
        /// <param name="warnings">The list of warnings to analyze.</param>
        /// <returns>A list of recommendations for improving symbol loading.</returns>
        public List<string> GetSymbolLoadingRecommendations(List<string> warnings)
        {
            var recommendations = new List<string>();

            if (warnings.Count == 0)
                return recommendations;

            var hasChecksumWarnings = warnings.Any(w => w.Contains("verify checksum"));
            var hasThirdPartyWarnings = warnings.Any(w =>
            {
                var moduleName = ExtractModuleNameFromWarning(w);
                return !string.IsNullOrWhiteSpace(moduleName) && IsThirdPartyModule(moduleName);
            });

            if (hasChecksumWarnings)
            {
                recommendations.Add("Consider updating symbol files for affected modules");
                recommendations.Add("Verify symbol server connectivity and authentication");
            }

            if (hasThirdPartyWarnings)
            {
                recommendations.Add("Third-party software symbols may not be available on public symbol servers");
                recommendations.Add("Contact software vendor for symbol files if detailed debugging is needed");
            }

            recommendations.Add("Use .sympath command to verify symbol search path configuration");
            recommendations.Add("Check symbol server timeout settings for slow connections");

            return recommendations;
        }

        /// <summary>
        /// Validates symbol server configuration based on warnings.
        /// </summary>
        /// <param name="symbolPath">The symbol search path to validate.</param>
        /// <param name="warnings">The warnings to analyze.</param>
        /// <returns>A validation result with recommendations.</returns>
        public SymbolServerValidationResult ValidateSymbolServerConfiguration(string? symbolPath, List<string> warnings)
        {
            var result = new SymbolServerValidationResult
            {
                IsValid = true,
                Recommendations = new List<string>(),
                Warnings = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(symbolPath))
            {
                result.IsValid = false;
                result.Warnings.Add("Symbol search path is not configured");
                result.Recommendations.Add("Configure symbol search path in application settings");
                return result;
            }

            // Check for common symbol server issues
            if (!symbolPath.Contains("srv*"))
            {
                result.Warnings.Add("No symbol server (srv*) configured in symbol path");
                result.Recommendations.Add("Add symbol server configuration to symbol search path");
            }

            if (!symbolPath.Contains("cache*"))
            {
                result.Warnings.Add("No local cache configured in symbol path");
                result.Recommendations.Add("Add local cache directory to improve symbol loading performance");
            }

            // Analyze warnings for specific issues
            var recommendations = GetSymbolLoadingRecommendations(warnings);
            result.Recommendations.AddRange(recommendations);

            return result;
        }
    }

    /// <summary>
    /// Represents the result of symbol server configuration validation.
    /// </summary>
    public class SymbolServerValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of recommendations for improving the configuration.
        /// </summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of warnings about the configuration.
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
}
