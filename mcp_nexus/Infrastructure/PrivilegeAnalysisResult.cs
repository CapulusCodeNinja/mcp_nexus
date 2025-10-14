using System;
using System.Collections.Generic;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Result of privilege analysis for service installation.
    /// Contains information about the current user's privileges and permissions required for service operations.
    /// </summary>
    public class PrivilegeAnalysisResult
    {
        /// <summary>
        /// Gets or sets whether the current user has all required privileges for service installation.
        /// </summary>
        public bool HasRequiredPrivileges { get; set; }

        /// <summary>
        /// Gets or sets whether the current user is running as an administrator.
        /// </summary>
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// Gets or sets the list of privileges that are missing for service installation.
        /// </summary>
        public string[] MissingPrivileges { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of privileges required for service installation.
        /// </summary>
        public string[] RequiredPrivileges { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of privileges currently available to the user.
        /// </summary>
        public string[] AvailablePrivileges { get; set; } = [];

        /// <summary>
        /// Gets or sets any error message that occurred during privilege analysis.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a dictionary mapping privilege names to their availability status.
        /// </summary>
        public Dictionary<string, bool> PrivilegeStatus { get; set; } = [];

        /// <summary>
        /// Gets or sets the date and time when the privilege analysis was performed.
        /// </summary>
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    }
}
