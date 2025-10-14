using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.Protocol;
using mcp_nexus.Tools;
using mcp_nexus.Extensions;
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
            // Ensure current NLog configuration (including MemoryTarget in tests) is active
            LogManager.ReconfigExistingLoggers();
            var bootstrapLogger = LogManager.GetCurrentClassLogger();
            bootstrapLogger.Info("Registering services...");

            RegisterCoreServices(services, configuration, customCdbPath, serviceMode);
            RegisterRecoveryServices(services);
            RegisterToolsAndProtocol(services);
            RegisterExtensionServices(services, configuration);

            bootstrapLogger.Info("All services registered successfully");
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

            // Register utility services for path handling and command preprocessing
            services.AddSingleton<mcp_nexus.Utilities.IWslPathConverter, mcp_nexus.Utilities.WslPathConverter>();
            services.AddSingleton<mcp_nexus.Utilities.IPathHandler, mcp_nexus.Utilities.PathHandler>();
            services.AddSingleton<mcp_nexus.Utilities.ICommandPreprocessor, mcp_nexus.Utilities.CommandPreprocessor>();

            logger.Info("Registered core services (CDB, Session, Notifications, Protocol, Utilities)");
        }


        /// <summary>
        /// Registers recovery and timeout services.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        private static void RegisterRecoveryServices(IServiceCollection services)
        {
            var logger = LogManager.GetCurrentClassLogger();
            services.AddSingleton<ICommandTimeoutService, CommandTimeoutService>();
            logger.Info("Registered CommandTimeoutService for automated timeouts");

            services.AddSingleton<ICdbSessionRecoveryService>(serviceProvider =>
            {
                var cdbSession = serviceProvider.GetRequiredService<ICdbSession>();
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSessionRecoveryService>>();
                var notificationService = serviceProvider.GetService<IMcpNotificationService>();
                var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

                // Create a callback that works with the session manager to cancel commands
                int cancelAllCommandsCallback(string reason)
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
                }

                return new CdbSessionRecoveryService(cdbSession, logger, cancelAllCommandsCallback, notificationService);
            });

            logger.Info("Registered recovery services");
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
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Registered command queue services");
        }

        /// <summary>
        /// Registers extension services for script-based workflows
        /// </summary>
        /// <param name="services">The service collection to register services with</param>
        /// <param name="configuration">The application configuration</param>
        private static void RegisterExtensionServices(IServiceCollection services, IConfiguration configuration)
        {
            // Get extensions configuration
            var extensionsEnabled = configuration.GetValue<bool>("McpNexus:Extensions:Enabled", true);
            var extensionsPath = configuration.GetValue<string>("McpNexus:Extensions:ExtensionsPath") ?? "extensions";
            var callbackPort = configuration.GetValue<int>("McpNexus:Extensions:CallbackPort", 0); // 0 = use MCP server port

            var logger = LogManager.GetCurrentClassLogger();
            if (!extensionsEnabled)
            {
                logger.Info("Extensions are disabled in configuration");
                return;
            }

            // Make extensions path absolute if relative
            if (!Path.IsPathRooted(extensionsPath))
            {
                extensionsPath = Path.Combine(AppContext.BaseDirectory, extensionsPath);
            }

            // Determine callback URL based on MCP server configuration (use McpNexus:Server settings)
            var mcpServerHost = configuration.GetValue<string>("McpNexus:Server:Host") ?? "localhost";
            var mcpServerPort = configuration.GetValue<int>("McpNexus:Server:Port", 5000);
            var actualCallbackPort = callbackPort > 0 ? callbackPort : mcpServerPort;
            var callbackUrl = $"http://127.0.0.1:{actualCallbackPort}/extension-callback";

            // Register extension services
            services.AddSingleton<IProcessWrapper, ProcessWrapper>();

            services.AddSingleton<IExtensionManager>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ExtensionManager>>();
                return new ExtensionManager(logger, extensionsPath);
            });

            services.AddSingleton<IExtensionExecutor>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ExtensionExecutor>>();
                var extensionManager = serviceProvider.GetRequiredService<IExtensionManager>();
                var processWrapper = serviceProvider.GetRequiredService<IProcessWrapper>();
                var tokenValidator = serviceProvider.GetRequiredService<IExtensionTokenValidator>();
                return new ExtensionExecutor(logger, extensionManager, callbackUrl, processWrapper, tokenValidator);
            });

            services.AddSingleton<IExtensionTokenValidator, ExtensionTokenValidator>();
            services.AddSingleton<IExtensionCommandTracker, ExtensionCommandTracker>();

            // Ensure extensions are loaded at startup so tools can find them immediately
            services.AddHostedService<ExtensionStartupService>();

            // Add controller
            services.AddControllers()
                .AddApplicationPart(typeof(ExtensionCallbackController).Assembly)
                .AddControllersAsServices();

            logger.Info($"Registered extension services (Path: {extensionsPath}, Callback: {callbackUrl})");
        }
    }
}
