namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Contains detailed results of privilege analysis for service operations
    /// </summary>
    public class PrivilegeAnalysisResult
    {
        /// <summary>
        /// The name of the current user
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The authentication type used
        /// </summary>
        public string AuthenticationType { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user is authenticated
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Whether the user is in the Administrators group
        /// </summary>
        public bool IsInAdministratorsGroup { get; set; }

        /// <summary>
        /// Whether the user is in the Power Users group
        /// </summary>
        public bool IsInPowerUsersGroup { get; set; }

        /// <summary>
        /// Whether the user is in the Users group
        /// </summary>
        public bool IsInUsersGroup { get; set; }

        /// <summary>
        /// The token elevation type for the current process
        /// </summary>
        public TokenElevationType TokenElevationType { get; set; }

        /// <summary>
        /// Whether the process token is fully elevated
        /// </summary>
        public bool IsElevated { get; set; }

        /// <summary>
        /// Whether the process can access the Service Control Manager
        /// </summary>
        public bool CanAccessServiceControlManager { get; set; }

        /// <summary>
        /// Whether the process can write to the installation directory
        /// </summary>
        public bool CanWriteToInstallDirectory { get; set; }

        /// <summary>
        /// Whether the process can access service-related registry keys
        /// </summary>
        public bool CanAccessRegistry { get; set; }

        /// <summary>
        /// Whether the process has sufficient privileges for service operations
        /// </summary>
        public bool HasSufficientPrivileges { get; set; }

        /// <summary>
        /// Error message if privilege analysis failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
