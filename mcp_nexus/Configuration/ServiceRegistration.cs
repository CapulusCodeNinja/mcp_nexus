using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.Protocol;
using mcp_nexus.Tools;
using NLog;
using System.Collections.Concurrent;

namespace mcp_nexus.Configuration
{
    /// <summary>
    /// Handles registration of core application services
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all application services with the DI container
        /// </summary>
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration, string? customCdbPath)
        {
            Console.Error.WriteLine("Registering services...");

            RegisterCoreServices(services, configuration, customCdbPath);
            RegisterAdvancedServices(services);
            RegisterRecoveryServices(services);
            RegisterToolsAndProtocol(services);

            Console.Error.WriteLine("All services registered successfully");
        }

        /// <summary>
        /// Registers core services (debugger, session management, etc.)
        /// </summary>
        private static void RegisterCoreServices(IServiceCollection services, IConfiguration configuration, string? customCdbPath)
        {
            // Get NLog logger for service registration
            var logger = LogManager.GetCurrentClassLogger();

            // Configure CDB session options with simple auto-detection
            services.Configure<CdbSessionOptions>(options =>
            {
                options.CommandTimeoutMs = configuration.GetValue<int>("McpNexus:Debugging:CommandTimeoutMs");
                options.SymbolServerTimeoutMs = configuration.GetValue<int>("McpNexus:Debugging:SymbolServerTimeoutMs");
                options.SymbolServerMaxRetries = configuration.GetValue<int>("McpNexus:Debugging:SymbolServerMaxRetries");
                options.SymbolSearchPath = configuration.GetValue<string>("McpNexus:Debugging:SymbolSearchPath");

                // Simple logic: Use configured path if file exists, otherwise auto-detect
                var configuredPath = customCdbPath ?? configuration.GetValue<string>("McpNexus:Debugging:CdbPath");

                if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
                {
                    // Valid configured path - use it
                    options.CustomCdbPath = configuredPath;
                    logger.Info("✅ CDB used from config: {CdbPath}", configuredPath);
                }
                else
                {
                    // Log why we're auto-detecting
                    if (!string.IsNullOrWhiteSpace(configuredPath))
                    {
                        logger.Warn("⚠️ Configured CDB '{ConfiguredPath}' not found - use autodetect", configuredPath);
                    }
                    else
                    {
                        logger.Info("🔍 No CDB path configured - use autodetect");
                    }

                    // Auto-detect CDB path
                    var cdbConfig = new mcp_nexus.Debugger.CdbSessionConfiguration(
                        commandTimeoutMs: options.CommandTimeoutMs,
                        customCdbPath: null, // Force auto-detection
                        symbolServerTimeoutMs: options.SymbolServerTimeoutMs,
                        symbolServerMaxRetries: options.SymbolServerMaxRetries,
                        symbolSearchPath: options.SymbolSearchPath ?? "",
                        startupDelayMs: configuration.GetValue<int>("McpNexus:Debugging:StartupDelayMs")
                    );
                    options.CustomCdbPath = cdbConfig.FindCdbPath();

                    if (options.CustomCdbPath != null)
                    {
                        logger.Info("✅ CDB used from auto-detect: {CdbPath}", options.CustomCdbPath);
                    }
                    else
                    {
                        logger.Error("❌ CDB auto-detect failed - no CDB found");
                    }
                }
            });

            // Configure session management options
            services.Configure<SessionConfiguration>(configuration.GetSection("McpNexus:SessionManagement"));

            // Register core services
            // Shared session store (explicit DI singleton instead of static state)
            services.AddSingleton(new ConcurrentDictionary<string, SessionInfo>());
            services.AddSingleton<ICdbSession, CdbSession>();
            services.AddSingleton<ISessionManager, ThreadSafeSessionManager>();
            services.AddSingleton<SessionAwareWindbgTool>();
            services.AddSingleton<IMcpNotificationService, McpNotificationService>();
            services.AddSingleton<IMcpToolDefinitionService, McpToolDefinitionService>();

            Console.Error.WriteLine("Registered core services (CDB, Session, Notifications, Protocol)");
        }

        /// <summary>
        /// Registers advanced services for performance and reliability
        /// </summary>
        private static void RegisterAdvancedServices(IServiceCollection services)
        {
            services.AddSingleton<mcp_nexus.Metrics.AdvancedMetricsService>();
            Console.Error.WriteLine("Registered AdvancedMetricsService for comprehensive performance monitoring");

            services.AddSingleton<mcp_nexus.Resilience.CircuitBreakerService>();
            Console.Error.WriteLine("Registered CircuitBreakerService for advanced fault tolerance");

            services.AddSingleton<mcp_nexus.Caching.IntelligentCacheService<string, object>>();
            Console.Error.WriteLine("Registered IntelligentCacheService for memory optimization");

            services.AddSingleton<mcp_nexus.Security.AdvancedSecurityService>();
            Console.Error.WriteLine("Registered AdvancedSecurityService for input validation and threat detection");

            services.AddSingleton<mcp_nexus.Health.AdvancedHealthService>();
            Console.Error.WriteLine("Registered AdvancedHealthService for comprehensive system monitoring");
        }

        /// <summary>
        /// Registers recovery and timeout services
        /// </summary>
        private static void RegisterRecoveryServices(IServiceCollection services)
        {
            services.AddSingleton<ICommandTimeoutService, CommandTimeoutService>();
            Console.Error.WriteLine("Registered CommandTimeoutService for automated timeouts");

            services.AddSingleton<ICdbSessionRecoveryService>(serviceProvider =>
            {
                var cdbSession = serviceProvider.GetRequiredService<ICdbSession>();
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSessionRecoveryService>>();
                var notificationService = serviceProvider.GetService<IMcpNotificationService>();
                var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

                // Create a callback that works with the session manager to cancel commands
                Func<string, int> cancelAllCommandsCallback = reason =>
                {
                    var sessions = sessionManager.GetAllSessions();
                    int totalCancelled = 0;

                    foreach (var session in sessions)
                    {
                        try
                        {
                            var commandQueue = sessionManager.GetCommandQueue(session.SessionId);
                            if (commandQueue != null)
                            {
                                totalCancelled += commandQueue.CancelAllCommands(reason);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error cancelling commands for session {SessionId}", session.SessionId);
                        }
                    }

                    return totalCancelled;
                };

                return new CdbSessionRecoveryService(cdbSession, logger, cancelAllCommandsCallback, notificationService);
            });

            Console.Error.WriteLine("Registered recovery services");
        }

        /// <summary>
        /// Registers tools and protocol services
        /// </summary>
        private static void RegisterToolsAndProtocol(IServiceCollection services)
        {
            // Command queue services
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            services.AddSingleton<ResilientCommandQueueService>();
            // Note: IsolatedCommandQueueService is created per session, not registered as singleton

            Console.Error.WriteLine("Registered command queue services");
        }
    }
}
