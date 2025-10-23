namespace nexus.engine.batch.Configuration;

/// <summary>
/// Configuration settings for command batching.
/// </summary>
internal class BatchingConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether command batching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of commands required to form a batch.
    /// </summary>
    public int MinBatchSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum number of commands that can be included in a single batch.
    /// </summary>
    public int MaxBatchSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the list of commands that should be excluded from batching.
    /// </summary>
    public List<string> ExcludedCommands { get; set; } = new()
    {
        "!analyze",
        "!dump",
        "!heap",
        "!memusage",
        "!runaway",
        "~*k",
        "!locks",
        "!cs",
        "!gchandles"
    };
}

