using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;
using Microsoft.Win32;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages Windows service registry operations, SC commands, and service cleanup
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServiceRegistryManager
    {
        /// <summary>
        /// Checks if the service is currently installed
        /// </summary>
        /// <returns>True if the service is installed, false otherwise</returns>
        public static bool IsServiceInstalled()
        {
            try
            {
                using var service = new ServiceController(ServiceConfiguration.ServiceName);
                // Accessing ServiceName will throw if service doesn't exist
                _ = service.ServiceName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Runs an SC (Service Control) command with the specified arguments
        /// </summary>
        /// <param name="arguments">The SC command arguments</param>
        /// <param name="logger">Optional logger for command output</param>
        /// <param name="allowFailure">If true, doesn't log errors on command failure</param>
        /// <returns>True if the command succeeded, false otherwise</returns>
        public static async Task<bool> RunScCommandAsync(string arguments, ILogger? logger = null, bool allowFailure = false)
        {
            try
            {
                OperationLogger.LogDebug(logger, OperationLogger.Operations.Install, "Running SC command: sc {Arguments}", arguments);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    if (!allowFailure)
                        OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Failed to start SC process");
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    OperationLogger.LogDebug(logger, OperationLogger.Operations.Install, "SC command succeeded: {Output}", output.Trim());
                    return true;
                }
                else
                {
                    if (!allowFailure)
                    {
                        OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                            "SC command failed with exit code {ExitCode}. Output: {Output}. Error: {Error}",
                            process.ExitCode, output.Trim(), error.Trim());
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (!allowFailure)
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                        "Exception running SC command '{Arguments}': {Error}", arguments, ex.Message);
                }
                return false;
            }
        }

        /// <summary>
        /// Attempts to force cleanup a service that may be marked for deletion
        /// </summary>
        /// <param name="logger">Optional logger for cleanup operations</param>
        /// <returns>True if cleanup was successful, false otherwise</returns>
        public static async Task<bool> ForceCleanupServiceAsync(ILogger? logger = null)
        {
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Attempting force cleanup of service registration");

            try
            {
                // Method 1: Try direct registry cleanup
                if (await DirectRegistryCleanupAsync(logger))
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Direct registry cleanup successful");
                    await Task.Delay(ServiceConfiguration.ServiceCleanupDelayMs);
                    return true;
                }

                // Method 2: Try alternative SC command sequence
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Trying alternative cleanup methods");

                // Stop any running instance
                await RunScCommandAsync(ServiceConfiguration.GetServiceStopCommand(), logger, allowFailure: true);
                await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

                // Try to delete again
                await RunScCommandAsync(ServiceConfiguration.GetDeleteServiceCommand(), logger, allowFailure: true);
                await Task.Delay(ServiceConfiguration.ServiceDeleteDelayMs);

                // Check if cleanup was successful
                if (!IsServiceInstalled())
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Alternative cleanup method successful");
                    return true;
                }

                // Method 3: Registry cleanup with more aggressive approach
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Trying aggressive registry cleanup");
                return await AggressiveRegistryCleanupAsync(logger);
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                    "Error during force cleanup: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Performs direct registry cleanup of service entries
        /// </summary>
        /// <param name="logger">Optional logger for cleanup operations</param>
        /// <returns>True if registry cleanup was successful, false otherwise</returns>
        public static async Task<bool> DirectRegistryCleanupAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Attempting direct registry cleanup");

                var serviceKeyPath = $@"SYSTEM\CurrentControlSet\Services\{ServiceConfiguration.ServiceName}";

                // Try to delete the service registry key
                try
                {
                    using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", true);
                    if (servicesKey?.GetSubKeyNames().Contains(ServiceConfiguration.ServiceName) == true)
                    {
                        servicesKey.DeleteSubKeyTree(ServiceConfiguration.ServiceName, false);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                            "Successfully deleted service registry key: {ServiceKeyPath}", serviceKeyPath);
                    }
                }
                catch (Exception ex)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                        "Could not delete service registry key {ServiceKeyPath}: {Error}", serviceKeyPath, ex.Message);
                }

                // Also try to clean up any event log sources
                try
                {
                    if (EventLog.SourceExists(ServiceConfiguration.ServiceName))
                    {
                        EventLog.DeleteEventSource(ServiceConfiguration.ServiceName);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                            "Deleted event log source: {ServiceName}", ServiceConfiguration.ServiceName);
                    }
                }
                catch (Exception ex)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                        "Could not delete event log source: {Error}", ex.Message);
                }

                // Wait for registry changes to take effect
                await Task.Delay(ServiceConfiguration.ServiceCleanupDelayMs);

                // Check if service is still installed
                var stillInstalled = IsServiceInstalled();
                if (!stillInstalled)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Direct registry cleanup successful");
                    return true;
                }
                else
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, "Service still appears to be installed after registry cleanup");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                    "Error during direct registry cleanup: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Performs aggressive registry cleanup when standard methods fail
        /// </summary>
        /// <param name="logger">Optional logger for cleanup operations</param>
        /// <returns>True if aggressive cleanup was successful, false otherwise</returns>
        private static async Task<bool> AggressiveRegistryCleanupAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Performing aggressive registry cleanup");

                // Clean up multiple registry locations
                var registryPaths = new[]
                {
                    $@"SYSTEM\CurrentControlSet\Services\{ServiceConfiguration.ServiceName}",
                    $@"SYSTEM\ControlSet001\Services\{ServiceConfiguration.ServiceName}",
                    $@"SYSTEM\ControlSet002\Services\{ServiceConfiguration.ServiceName}"
                };

                foreach (var path in registryPaths)
                {
                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(path.Substring(0, path.LastIndexOf('\\')), true);
                        var serviceName = path.Substring(path.LastIndexOf('\\') + 1);

                        if (key?.GetSubKeyNames().Contains(serviceName) == true)
                        {
                            key.DeleteSubKeyTree(serviceName, false);
                            OperationLogger.LogDebug(logger, OperationLogger.Operations.Install,
                                "Deleted registry key: {Path}", path);
                        }
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogDebug(logger, OperationLogger.Operations.Install,
                            "Could not delete registry key {Path}: {Error}", path, ex.Message);
                    }
                }

                // Wait for changes to take effect
                await Task.Delay(ServiceConfiguration.ServiceCleanupDelayMs);

                var stillInstalled = IsServiceInstalled();
                if (!stillInstalled)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Aggressive registry cleanup successful");
                    return true;
                }
                else
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                        "Service still appears to be installed after aggressive cleanup");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                    "Error during aggressive registry cleanup: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Creates the Windows service registration
        /// </summary>
        /// <param name="logger">Optional logger for installation operations</param>
        /// <returns>True if service creation was successful, false otherwise</returns>
        public static async Task<bool> CreateServiceAsync(ILogger? logger = null)
        {
            var installCommand = ServiceConfiguration.GetCreateServiceCommand(ServiceConfiguration.ExecutablePath);
            var result = await RunScCommandAsync(installCommand, logger);

            if (result)
            {
                // Set service description
                var descriptionCommand = ServiceConfiguration.GetServiceDescriptionCommand();
                await RunScCommandAsync(descriptionCommand, logger, allowFailure: true);
            }

            return result;
        }

        /// <summary>
        /// Deletes the Windows service registration
        /// </summary>
        /// <param name="logger">Optional logger for uninstallation operations</param>
        /// <returns>True if service deletion was successful, false otherwise</returns>
        public static async Task<bool> DeleteServiceAsync(ILogger? logger = null)
        {
            // First try to stop the service
            await RunScCommandAsync(ServiceConfiguration.GetServiceStopCommand(), logger, allowFailure: true);
            await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

            // Then delete it
            var result = await RunScCommandAsync(ServiceConfiguration.GetDeleteServiceCommand(), logger);

            if (result)
            {
                await Task.Delay(ServiceConfiguration.ServiceDeleteDelayMs);
            }

            return result;
        }
    }
}
