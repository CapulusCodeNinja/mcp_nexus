namespace Nexus.Config.Models;

/// <summary>
/// Command batching configuration settings.
/// </summary>
public class BatchingSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether batching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size.
    /// </summary>
    public int MaxBatchSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum batch size.
    /// </summary>
    public int MinBatchSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the batch wait timeout in milliseconds.
    /// </summary>
    public int BatchWaitTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the batch timeout multiplier.
    /// </summary>
    public double BatchTimeoutMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the maximum batch timeout in minutes.
    /// </summary>
    public int MaxBatchTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the list of excluded commands from batching.
    /// </summary>
    public List<string> ExcludedCommands { get; set; } = new();

    /// <summary>
    /// Gets or sets the command collection wait timeout in milliseconds.
    /// This controls how long to wait for additional commands before processing a batch.
    /// Set to 0 for no artificial delay (only natural batching).
    /// Default: 0 (instant, no wait).
    /// </summary>
    public int CommandCollectionWaitMs { get; set; } = 0;
}
