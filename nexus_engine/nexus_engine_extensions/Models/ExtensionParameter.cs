using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Models;

/// <summary>
/// Defines a parameter that an extension accepts.
/// </summary>
internal class ExtensionParameter
{
    /// <summary>
    /// Gets or sets name of the parameter.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets type of the parameter (e.g., "string", "int", "bool").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    /// <summary>
    /// Gets or sets description of the parameter.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether whether the parameter is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    /// <summary>
    /// Gets or sets default value for the parameter.
    /// </summary>
    [JsonPropertyName("default")]
    public object? Default
    {
        get; set;
    }
}
