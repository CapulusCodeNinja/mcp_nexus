using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Models;

/// <summary>
/// Defines a parameter that an extension accepts.
/// </summary>
public class ExtensionParameter
{
    /// <summary>
    /// Name of the parameter.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of the parameter (e.g., "string", "int", "bool").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>
    /// Description of the parameter.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether the parameter is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    /// <summary>
    /// Default value for the parameter.
    /// </summary>
    [JsonPropertyName("default")]
    public object? Default
    {
        get; set;
    }
}

