namespace mcp_nexus.Constants
{
    /// <summary>
    /// Application-wide constants to avoid magic numbers and improve maintainability
    /// </summary>
    public static class ApplicationConstants
    {
        // Command timeout constants
        /// <summary>
        /// Timeout for simple commands that should complete quickly.
        /// </summary>
        public static readonly TimeSpan SimpleCommandTimeout = TimeSpan.FromMinutes(2);
        
        /// <summary>
        /// Default timeout for most commands.
        /// </summary>
        public static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Maximum timeout for complex commands.
        /// </summary>
        public static readonly TimeSpan MaxCommandTimeout = TimeSpan.FromMinutes(30);
        
        /// <summary>
        /// Timeout for long-running commands that may take significant time.
        /// </summary>
        public static readonly TimeSpan LongRunningCommandTimeout = TimeSpan.FromHours(1);

        // Cleanup and retention constants
        /// <summary>
        /// Interval for cleaning up expired commands and resources.
        /// </summary>
        public static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// How long to retain completed commands before cleanup.
        /// </summary>
        public static readonly TimeSpan CommandRetentionTime = TimeSpan.FromHours(1);

        // Polling constants
        /// <summary>
        /// Initial polling interval for checking command completion.
        /// </summary>
        public static readonly TimeSpan InitialPollInterval = TimeSpan.FromMilliseconds(100);
        
        /// <summary>
        /// Maximum polling interval to prevent excessive CPU usage.
        /// </summary>
        public static readonly TimeSpan MaxPollInterval = TimeSpan.FromSeconds(2);
        
        /// <summary>
        /// Multiplier for exponential backoff in polling.
        /// </summary>
        public static readonly double PollBackoffMultiplier = 1.5;

        // Server configuration constants
        /// <summary>
        /// Default HTTP port for the application.
        /// </summary>
        public static readonly int DefaultHttpPort = 5000;
        
        /// <summary>
        /// Default development port for the application.
        /// </summary>
        public static readonly int DefaultDevPort = 5117;
        
        /// <summary>
        /// Default service port for the application.
        /// </summary>
        public static readonly int DefaultServicePort = 5511;

        // Process management constants
        /// <summary>
        /// Timeout for waiting for process operations to complete.
        /// </summary>
        public static readonly TimeSpan ProcessWaitTimeout = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// Timeout for service shutdown operations.
        /// </summary>
        public static readonly TimeSpan ServiceShutdownTimeout = TimeSpan.FromSeconds(5);

        // Logging constants
        /// <summary>
        /// Interval for logging statistics and performance metrics.
        /// </summary>
        public static readonly TimeSpan StatsLogInterval = TimeSpan.FromMinutes(5);

        // File path constants
        /// <summary>
        /// Maximum length for displaying file paths in logs.
        /// </summary>
        public const int MaxPathDisplayLength = 50;
        
        /// <summary>
        /// Number of characters to show before truncation (for "..." prefix).
        /// </summary>
        public const int PathTruncationPrefix = 3; // "..."

        // HTTP configuration constants
        /// <summary>
        /// Timeout for HTTP requests.
        /// </summary>
        public static readonly TimeSpan HttpRequestTimeout = TimeSpan.FromMinutes(15);
        
        /// <summary>
        /// Keep-alive timeout for HTTP connections.
        /// </summary>
        public static readonly TimeSpan HttpKeepAliveTimeout = TimeSpan.FromMinutes(15);

        // Recovery constants
        /// <summary>
        /// Maximum number of recovery attempts for failed operations.
        /// </summary>
        public static readonly int MaxRecoveryAttempts = 3;
        
        /// <summary>
        /// Delay between recovery attempts.
        /// </summary>
        public static readonly TimeSpan RecoveryDelay = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// Interval for performing health checks.
        /// </summary>
        public static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(30);

        // Performance Optimization Settings
        /// <summary>
        /// Maximum number of concurrent notifications to process.
        /// </summary>
        public const int MaxConcurrentNotifications = 10;
        
        /// <summary>
        /// Maximum batch size for cleanup operations.
        /// </summary>
        public const int MaxCleanupBatchSize = 100;
        
        /// <summary>
        /// Maximum length for log messages before truncation.
        /// </summary>
        public const int MaxLogMessageLength = 1000;

        // CDB Session timing constants
        /// <summary>
        /// Delay for CDB interrupt operations.
        /// </summary>
        public static readonly TimeSpan CdbInterruptDelay = TimeSpan.FromMilliseconds(1000);
        
        /// <summary>
        /// Delay for waiting for CDB prompt.
        /// </summary>
        public static readonly TimeSpan CdbPromptDelay = TimeSpan.FromMilliseconds(2000);
        
        /// <summary>
        /// Delay for CDB startup operations.
        /// </summary>
        public static readonly TimeSpan CdbStartupDelay = TimeSpan.FromMilliseconds(200);
        
        /// <summary>
        /// Delay for CDB output processing.
        /// </summary>
        public static readonly TimeSpan CdbOutputDelay = TimeSpan.FromMilliseconds(500);
        
        /// <summary>
        /// Delay between CDB commands.
        /// </summary>
        public static readonly TimeSpan CdbCommandDelay = TimeSpan.FromMilliseconds(1000);
        /// <summary>
        /// Timeout for CDB output operations.
        /// </summary>
        public static readonly TimeSpan CdbOutputTimeout = TimeSpan.FromMilliseconds(5000);
        
        /// <summary>
        /// Timeout for waiting for CDB process operations.
        /// </summary>
        public static readonly TimeSpan CdbProcessWaitTimeout = TimeSpan.FromMilliseconds(5000);

        // Memory display constants
        /// <summary>
        /// Number of bytes per megabyte for memory calculations.
        /// </summary>
        public const double BytesPerMB = 1024.0 * 1024.0;
    }
}

