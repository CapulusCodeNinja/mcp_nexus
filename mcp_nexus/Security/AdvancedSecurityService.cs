using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Security
{
    /// <summary>
    /// Advanced security service for input validation and threat detection
    /// </summary>
    public class AdvancedSecurityService
    {
        private readonly ILogger<AdvancedSecurityService> m_logger;
        private readonly HashSet<string> m_dangerousCommands;
        private readonly Regex m_pathTraversalRegex;
        private readonly Regex m_sqlInjectionRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedSecurityService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording security operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public AdvancedSecurityService(ILogger<AdvancedSecurityService> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize dangerous command patterns
            m_dangerousCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "format", "fdisk", "del", "rmdir", "rd", "rm", "shutdown", "restart",
                "net user", "net localgroup", "reg add", "reg delete", "wmic",
                "powershell", "cmd", "bash", "sh", "exec", "system"
            };

            // Path traversal patterns
            m_pathTraversalRegex = new Regex(@"(\.\./|\.\.\\|\.\.%2f|\.\.%5c)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Basic SQL injection patterns
            m_sqlInjectionRegex = new Regex(@"(union|select|insert|update|delete|drop|create|alter|exec|execute)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            m_logger.LogInformation("🔒 AdvancedSecurityService initialized");
        }

        /// <summary>
        /// Validates a command for security threats and dangerous patterns.
        /// Checks for dangerous commands, path traversal attempts, SQL injection patterns, and command length limits.
        /// </summary>
        /// <param name="command">The command string to validate.</param>
        /// <returns>
        /// A <see cref="SecurityValidationResult"/> indicating whether the command is safe to execute.
        /// Returns <see cref="SecurityValidationResult.Valid()"/> if the command passes all security checks;
        /// otherwise, returns <see cref="SecurityValidationResult.Invalid(string)"/> with details about the security issues found.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null, empty, or contains only whitespace.</exception>
        public SecurityValidationResult ValidateCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return SecurityValidationResult.Invalid("Command cannot be empty");
            }

            var trimmedCommand = command.Trim();
            var issues = new List<string>();

            // Check for dangerous commands
            foreach (var dangerous in m_dangerousCommands)
            {
                if (trimmedCommand.Contains(dangerous, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add($"Potentially dangerous command detected: {dangerous}");
                }
            }

            // Check for path traversal
            if (m_pathTraversalRegex.IsMatch(trimmedCommand))
            {
                issues.Add("Path traversal attempt detected");
            }

            // Check for SQL injection patterns
            if (m_sqlInjectionRegex.IsMatch(trimmedCommand))
            {
                issues.Add("SQL injection pattern detected");
            }

            // Check command length
            if (trimmedCommand.Length > 1000)
            {
                issues.Add("Command too long (max 1000 characters)");
            }

            if (issues.Count > 0)
            {
                m_logger.LogWarning("🔒 Security validation failed for command: {Command}, Issues: {Issues}",
                    command, string.Join("; ", issues));
                return SecurityValidationResult.Invalid(string.Join("; ", issues));
            }

            m_logger.LogTrace("🔒 Command passed security validation: {Command}", command);
            return SecurityValidationResult.Valid();
        }

        /// <summary>
        /// Validates a file path for security threats and access restrictions.
        /// Checks for path traversal attempts, validates allowed root directories, and verifies file extensions.
        /// </summary>
        /// <param name="filePath">The file path string to validate.</param>
        /// <returns>
        /// A <see cref="SecurityValidationResult"/> indicating whether the file path is safe to access.
        /// Returns <see cref="SecurityValidationResult.Valid()"/> if the path passes all security checks;
        /// otherwise, returns <see cref="SecurityValidationResult.Invalid(string)"/> with details about the security issues found.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null, empty, or contains only whitespace.</exception>
        public SecurityValidationResult ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return SecurityValidationResult.Invalid("File path cannot be empty");
            }

            var issues = new List<string>();

            // Check for path traversal
            if (m_pathTraversalRegex.IsMatch(filePath))
            {
                issues.Add("Path traversal attempt detected");
            }

            // Check for absolute paths outside allowed directories
            if (Path.IsPathRooted(filePath))
            {
                var allowedRoots = new[] { "C:\\", "D:\\", "E:\\" };
                var isAllowed = allowedRoots.Any(root => filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    issues.Add("File path outside allowed directories");
                }
            }

            // Check file extension
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var allowedExtensions = new[] { ".dmp", ".exe", ".dll", ".pdb", ".sym" };

            if (!string.IsNullOrEmpty(extension) && !allowedExtensions.Contains(extension))
            {
                issues.Add($"File extension not allowed: {extension}");
            }

            if (issues.Count > 0)
            {
                m_logger.LogWarning("🔒 File path validation failed: {FilePath}, Issues: {Issues}",
                    filePath, string.Join("; ", issues));
                return SecurityValidationResult.Invalid(string.Join("; ", issues));
            }

            return SecurityValidationResult.Valid();
        }

        /// <summary>
        /// Validates a session ID for proper format and security compliance.
        /// Ensures the session ID matches the expected pattern and is not empty or malformed.
        /// </summary>
        /// <param name="sessionId">The session ID string to validate.</param>
        /// <returns>
        /// A <see cref="SecurityValidationResult"/> indicating whether the session ID is valid.
        /// Returns <see cref="SecurityValidationResult.Valid()"/> if the session ID passes validation;
        /// otherwise, returns <see cref="SecurityValidationResult.Invalid(string)"/> with details about the validation failure.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is null, empty, or contains only whitespace.</exception>
        public SecurityValidationResult ValidateSessionId(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return SecurityValidationResult.Invalid("Session ID cannot be empty");
            }

            // Session ID should match expected pattern
            var sessionIdRegex = new Regex(@"^sess-\d{6}-[a-f0-9]{8}-[a-f0-9]{8}-\d{4}$", RegexOptions.Compiled);

            if (!sessionIdRegex.IsMatch(sessionId))
            {
                m_logger.LogWarning("🔒 Invalid session ID format: {SessionId}", sessionId);
                return SecurityValidationResult.Invalid("Invalid session ID format");
            }

            return SecurityValidationResult.Valid();
        }
    }

    /// <summary>
    /// Represents the result of a security validation operation.
    /// Contains information about whether the validation passed and any error messages.
    /// </summary>
    public class SecurityValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets the error message if validation failed, or null if validation passed.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityValidationResult"/> class.
        /// </summary>
        /// <param name="isValid">Whether the validation passed.</param>
        /// <param name="errorMessage">The error message if validation failed, or null if validation passed.</param>
        private SecurityValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a valid security validation result.
        /// </summary>
        /// <returns>A <see cref="SecurityValidationResult"/> indicating successful validation.</returns>
        public static SecurityValidationResult Valid() => new(true);

        /// <summary>
        /// Creates an invalid security validation result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing why validation failed.</param>
        /// <returns>A <see cref="SecurityValidationResult"/> indicating failed validation.</returns>
        public static SecurityValidationResult Invalid(string errorMessage) => new(false, errorMessage);
    }
}
