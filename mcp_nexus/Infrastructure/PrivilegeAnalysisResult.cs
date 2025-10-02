using System;
using System.Collections.Generic;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Result of privilege analysis for service installation
    /// </summary>
    public class PrivilegeAnalysisResult
    {
        public bool HasRequiredPrivileges { get; set; }
        public bool IsAdministrator { get; set; }
        public string[] MissingPrivileges { get; set; } = Array.Empty<string>();
        public string[] RequiredPrivileges { get; set; } = Array.Empty<string>();
        public string[] AvailablePrivileges { get; set; } = Array.Empty<string>();
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, bool> PrivilegeStatus { get; set; } = new Dictionary<string, bool>();
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    }
}
