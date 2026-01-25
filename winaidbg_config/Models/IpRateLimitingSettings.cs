namespace WinAiDbg.Config.Models;

/// <summary>
/// IP rate limiting configuration settings.
/// </summary>
public class IpRateLimitingSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether endpoint rate limiting is enabled.
    /// </summary>
    public bool EnableEndpointRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to stack blocked requests.
    /// </summary>
    public bool StackBlockedRequests { get; set; } = false;

    /// <summary>
    /// Gets or sets the real IP header.
    /// </summary>
    public string RealIpHeader { get; set; } = "X-Real-IP";

    /// <summary>
    /// Gets or sets the client ID header.
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-ClientId";

    /// <summary>
    /// Gets or sets the general rules.
    /// </summary>
    public List<RateLimitRule> GeneralRules { get; set; } = new();
}
