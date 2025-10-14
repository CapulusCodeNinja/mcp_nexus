using System.Text.Json.Serialization;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Metadata for an extension script that defines its capabilities and requirements.
    /// </summary>
    public class ExtensionMetadata
    {
        /// <summary>
        /// Unique name of the extension (e.g., "stack_with_sources").
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of what the extension does.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Version of the extension.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Author of the extension.
        /// </summary>
        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Script type (e.g., "powershell", "python").
        /// </summary>
        [JsonPropertyName("scriptType")]
        public string ScriptType { get; set; } = "powershell";

        /// <summary>
        /// Relative path to the script file from the extension directory.
        /// </summary>
        [JsonPropertyName("scriptFile")]
        public string ScriptFile { get; set; } = string.Empty;

        /// <summary>
        /// Maximum execution timeout in milliseconds (0 = no timeout).
        /// </summary>
        [JsonPropertyName("timeout")]
        public int Timeout { get; set; } = 1800000; // 30 minutes default

        /// <summary>
        /// List of required modules/dependencies.
        /// </summary>
        [JsonPropertyName("requires")]
        public List<string> Requires { get; set; } = [];

        /// <summary>
        /// Parameter definitions for the extension.
        /// </summary>
        [JsonPropertyName("parameters")]
        public List<ExtensionParameter> Parameters { get; set; } = [];

        /// <summary>
        /// Absolute path to the extension directory (set at runtime).
        /// </summary>
        [JsonIgnore]
        public string ExtensionPath { get; set; } = string.Empty;

        /// <summary>
        /// Absolute path to the script file (set at runtime).
        /// </summary>
        [JsonIgnore]
        public string FullScriptPath => Path.Combine(ExtensionPath, ScriptFile);
    }

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
        public object? Default { get; set; }
    }
}

