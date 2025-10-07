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
        /// Registers all application services with the DI container.
        /// Configures core services, advanced services, recovery services, and tools.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="customCdbPath">Optional custom path to the CDB executable.</param>
        /// <param name="serviceMode">Whether the application is running in service mode.</param>
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration, string? customCdbPath, bool serviceMode = false)
        {
            Console.Error.WriteLine("Registering services...");

            RegisterCoreServices(services, configuration, customCdbPath, serviceMode);
            RegisterRecoveryServices(services);
            RegisterToolsAndProtocol(services);

            Console.Error.WriteLine("All services registered successfully");
        }

        /// <summary>
        /// Registers core services (debugger, session management, etc.).
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="customCdbPath">Optional custom path to the CDB executable.</param>
        /// <param name="serviceMode">Whether the application is running in service mode.</param>
        private static void RegisterCoreServices(IServiceCollection services, IConfiguration configuration, string? customCdbPath, bool serviceMode)
        {
            // Get NLog logger for service registration
            var logger = LogManager.GetCurrentClassLogger();

            // CRITICAL: Detect CDB path BEFORE Configure block so logging works
            string? resolvedCdbPath = null;
            var configuredPath = customCdbPath ?? configuration.GetValue<string>("McpNexus:Debugging:CdbPath");

            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            {
                // Valid configured path - use it
                resolvedCdbPath = configuredPath;
                logger.Info("✅ CDB used from config: {CdbPath}", configuredPath);
            }
            else
            {
                // Log why we're auto-detecting
                if (!string.IsNullOrWhiteSpace(configuredPath))
                {
                    logger.Warn("⚠️ Configured CDB '{ConfiguredPath}' not found - will auto-detect", configuredPath);
                }
                else
                {
                    logger.Info("🔍 No CDB path configured - starting auto-detect");
                }

                try
                {
                    // Auto-detect CDB path SYNCHRONOUSLY during service registration
                    var cdbConfig = new mcp_nexus.Debugger.CdbSessionConfiguration(
                        commandTimeoutMs: configuration.GetValue<int>("McpNexus:Debugging:CommandTimeoutMs"),
                        idleTimeoutMs: configuration.GetValue<int>("McpNexus:Debugging:IdleTimeoutMs", 180000),
                        customCdbPath: null, // Force auto-detection
                        symbolServerMaxRetries: configuration.GetValue<int>("McpNexus:Debugging:SymbolServerMaxRetries"),
                        symbolSearchPath: configuration.GetValue<string>("McpNexus:Debugging:SymbolSearchPath") ?? "",
                        startupDelayMs: configuration.GetValue<int>("McpNexus:Debugging:StartupDelayMs", 1000),
                        outputReadingTimeoutMs: configuration.GetValue<int>("McpNexus:Debugging:OutputReadingTimeoutMs", 300000),
                        enableCommandPreprocessing: configuration.GetValue<bool>("McpNexus:Debugging:EnableCommandPreprocessing", true)
                    );
                    resolvedCdbPath = cdbConfig.FindCdbPath();

                    if (resolvedCdbPath != null)
                    {
                        logger.Info("✅ CDB auto-detect succeeded: {CdbPath}", resolvedCdbPath);
                    }
                    else
                    {
                        logger.Error("❌ CDB auto-detect failed - no CDB found in standard locations or PATH");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "💥 Exception during CDB auto-detection: {Message}", ex.Message);
                    resolvedCdbPath = null;
                }
            }

            // Now configure options with the resolved path
            services.Configure<CdbSessionOptions>(options =>
            {
                options.CommandTimeoutMs = configuration.GetValue<int>("McpNexus:Debugging:CommandTimeoutMs");
                options.IdleTimeoutMs = configuration.GetValue<int>("McpNexus:Debugging:IdleTimeoutMs", 180000);
                options.SymbolServerMaxRetries = configuration.GetValue<int>("McpNexus:Debugging:SymbolServerMaxRetries");
                options.SymbolSearchPath = configuration.GetValue<string>("McpNexus:Debugging:SymbolSearchPath");
                options.StartupDelayMs = configuration.GetValue<int>("McpNexus:Debugging:StartupDelayMs", 1000);
                options.OutputReadingTimeoutMs = configuration.GetValue<int>("McpNexus:Debugging:OutputReadingTimeoutMs", 300000);
                options.EnableCommandPreprocessing = configuration.GetValue<bool>("McpNexus:Debugging:EnableCommandPreprocessing", true);
                options.CustomCdbPath = resolvedCdbPath; // Use pre-resolved path
            });

            // Configure session management options
            var capturedServiceMode = serviceMode;
            services.Configure<SessionConfiguration>(config =>
            {
                configuration.GetSection("McpNexus:SessionManagement").Bind(config);
                config.ServiceMode = capturedServiceMode;
            });

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
        /// Registers recovery and timeout services.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
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
        /// <param name="services">The service collection to register services with</param>
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
