using System;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Information about a backup operation
    /// </summary>
    public class BackupInfo
    {
        public string Path { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public long Size { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string[] Files { get; set; } = Array.Empty<string>();
        public string ErrorMessage { get; set; } = string.Empty;

        // Additional properties expected by tests
        public DateTime CreationTime { get; set; }
        public long SizeBytes { get; set; }
    }
}
