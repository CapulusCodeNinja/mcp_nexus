using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Hosted service that ensures extensions are discovered and loaded during application startup.
    /// This guarantees that tools can enumerate and execute extensions immediately after the server starts.
    /// </summary>
    public sealed class ExtensionStartupService : IHostedService
    {
        private readonly ILogger<ExtensionStartupService> m_Logger;
        private readonly IExtensionManager m_ExtensionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionStartupService"/> class.
        /// </summary>
        /// <param name="logger">Logger for startup diagnostics.</param>
        /// <param name="extensionManager">The extension manager responsible for discovery and validation.</param>
        public ExtensionStartupService(
            ILogger<ExtensionStartupService> logger,
            IExtensionManager extensionManager)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_ExtensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
        }

        /// <summary>
        /// Triggers extension discovery and loading on application start.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for startup.</param>
        /// <returns>A task that completes when the extensions have been loaded.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                m_Logger.LogInformation("🔎 Loading extensions at startup...");
                await m_ExtensionManager.LoadExtensionsAsync();
                var count = m_ExtensionManager.GetAllExtensions().Count();
                m_Logger.LogInformation("✅ Extensions loaded: {Count}", count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Startup cancelled; swallow gracefully
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to load extensions during startup");
            }
        }

        /// <summary>
        /// No-op on shutdown; extension lifecycle is managed elsewhere.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for shutdown.</param>
        /// <returns>A completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}


