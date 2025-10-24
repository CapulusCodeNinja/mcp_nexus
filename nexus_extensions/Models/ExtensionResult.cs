using System.Text.Json.Serialization;

namespace nexus.extensions.Models;

/// <summary>
/// Result of an extension execution.
/// </summary>
public class ExtensionResult
{
    /// <summary>
    /// Whether the extension executed successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success
    {
        get; set;
    }

    /// <summary>
    /// Output from the extension (typically JSON).
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error
    {
        get; set;
    }

    /// <summary>
    /// Exit code of the extension process.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int ExitCode
    {
        get; set;
    }

    /// <summary>
    /// Total execution time.
    /// </summary>
    [JsonPropertyName("executionTime")]
    public TimeSpan ExecutionTime
    {
        get; set;
    }

    /// <summary>
    /// Standard error output from the extension.
    /// </summary>
    [JsonPropertyName("standardError")]
    public string? StandardError
    {
        get; set;
    }
}

