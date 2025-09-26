namespace mcp_nexus.Constants
{
    /// <summary>
    /// Application-wide constants to avoid magic numbers and improve maintainability
    /// </summary>
    public static class ApplicationConstants
    {
        // Command timeout constants
        public static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan MaxCommandTimeout = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan LongRunningCommandTimeout = TimeSpan.FromHours(1);
        
        // Cleanup and retention constants
        public static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan CommandRetentionTime = TimeSpan.FromHours(1);
        
        // Polling constants
        public static readonly TimeSpan InitialPollInterval = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan MaxPollInterval = TimeSpan.FromSeconds(2);
        public static readonly double PollBackoffMultiplier = 1.5;
        
        // Server configuration constants
        public static readonly int DefaultHttpPort = 5000;
        public static readonly int DefaultDevPort = 5117;
        public static readonly int DefaultServicePort = 5511;
        
        // Process management constants
        public static readonly TimeSpan ProcessWaitTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan ServiceShutdownTimeout = TimeSpan.FromSeconds(5);
        
        // Logging constants
        public static readonly TimeSpan StatsLogInterval = TimeSpan.FromMinutes(5);
        
        // File path constants
        public const int MaxPathDisplayLength = 50;
        public const int PathTruncationPrefix = 3; // "..."
        
        // HTTP configuration constants
        public static readonly TimeSpan HttpRequestTimeout = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan HttpKeepAliveTimeout = TimeSpan.FromMinutes(15);
        
        // Recovery constants
        public static readonly int MaxRecoveryAttempts = 3;
        public static readonly TimeSpan RecoveryDelay = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(30);

        // Performance Optimization Settings
        public const int MaxConcurrentNotifications = 10;
        public const int MaxCleanupBatchSize = 100;
        public const int MaxLogMessageLength = 1000;
        
        // CDB Session timing constants
        public static readonly TimeSpan CdbInterruptDelay = TimeSpan.FromMilliseconds(1000);
        public static readonly TimeSpan CdbPromptDelay = TimeSpan.FromMilliseconds(2000);
        public static readonly TimeSpan CdbStartupDelay = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan CdbOutputDelay = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan CdbCommandDelay = TimeSpan.FromMilliseconds(1000);
        public static readonly TimeSpan CdbOutputTimeout = TimeSpan.FromMilliseconds(5000);
        public static readonly TimeSpan CdbProcessWaitTimeout = TimeSpan.FromMilliseconds(5000);
        
        // Memory display constants
        public const double BytesPerMB = 1024.0 * 1024.0;
    }
}

