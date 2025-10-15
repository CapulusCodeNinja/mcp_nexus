using System;

namespace mcp_nexus.Infrastructure.Core
{
    /// <summary>
    /// Information about a backup operation.
    /// Contains metadata about backup files including path, size, creation time, and validation status.
    /// </summary>
    public class BackupInfo
    {
        /// <summary>
        /// Gets or sets the path to the backup directory or file.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation date and time of the backup.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the size of the backup in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the name of the backup.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the backup is valid and can be restored.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of files included in the backup.
        /// </summary>
        public string[] Files { get; set; } = [];

        /// <summary>
        /// Gets or sets any error message associated with the backup operation.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        // Additional properties expected by tests
        /// <summary>
        /// Gets or sets the creation time of the backup (alternative to Created property).
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the size of the backup in bytes (alternative to Size property).
        /// </summary>
        public long SizeBytes { get; set; }
    }
}

