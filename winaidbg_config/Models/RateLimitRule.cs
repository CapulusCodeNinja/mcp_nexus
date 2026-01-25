namespace WinAiDbg.Config.Models;

/// <summary>
/// Rate limit rule configuration.
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Gets or sets the endpoint pattern.
    /// </summary>
    public string Endpoint { get; set; } = "*:/";

    /// <summary>
    /// Gets or sets the period.
    /// </summary>
    public string Period { get; set; } = "1m";

    /// <summary>
    /// Gets or sets the limit.
    /// </summary>
    public int Limit { get; set; } = 100;
}
