using System.Text.Json;

namespace mcp_nexus.Utilities.Json
{
    /// <summary>
    /// Centralized JSON serializer options for consistent configuration across the application.
    /// </summary>
    public static class JsonOptions
    {
        private static readonly JsonSerializerOptions m_Indented = new() { WriteIndented = true };
        private static readonly JsonSerializerOptions m_Compact = new();

        public static JsonSerializerOptions JsonIndented => m_Indented;
        public static JsonSerializerOptions JsonCompact => m_Compact;
    }
}


