namespace Nexus.Config.Models;

/// <summary>
/// MCP Nexus specific configuration settings.
/// </summary>
public class McpNexusSettings
{
    /// <summary>
    /// Gets or sets the server configuration.
    /// </summary>
    public ServerSettings Server { get; set; } = new();

    /// <summary>
    /// Gets or sets the transport configuration.
    /// </summary>
    public TransportSettings Transport { get; set; } = new();

    /// <summary>
    /// Gets or sets the debugging configuration.
    /// </summary>
    public DebuggingSettings Debugging { get; set; } = new();

    /// <summary>
    /// Gets or sets the automated recovery configuration.
    /// </summary>
    public AutomatedRecoverySettings AutomatedRecovery { get; set; } = new();

    /// <summary>
    /// Gets or sets the service configuration.
    /// </summary>
    public ServiceSettings Service { get; set; } = new();

    /// <summary>
    /// Gets or sets the session management configuration.
    /// </summary>
    public SessionManagementSettings SessionManagement { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation configuration.
    /// </summary>
    public ValidationSettings Validation { get; set; } = new();

    /// <summary>
    /// Gets or sets the extensions settings.
    /// </summary>
    public ExtensionsSettings Extensions { get; set; } = new();

    /// <summary>
    /// Gets or sets the batching settings.
    /// </summary>
    public BatchingSettings Batching { get; set; } = new();

    /// <summary>
    /// Gets or sets the process statistics settings.
    /// </summary>
    public ProcessStatisticsSettings ProcessStatistics { get; set; } = new();
}
