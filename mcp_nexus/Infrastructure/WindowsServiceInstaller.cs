using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Windows service installer - maintains compatibility with existing code.
    /// Provides comprehensive Windows service installation, uninstallation, and management capabilities.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowsServiceInstaller
    {

        /// <summary>
        /// Installs the Windows service asynchronously
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public static async Task<bool> InstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Installing MCP Nexus service");

                // Build the project first
                var buildResult = await BuildProjectForDeploymentAsync(logger);
                if (!buildResult)
                {
                    logger?.LogError("Build failed during service installation");
                    return false;
                }

                // Copy files to installation directory
                var copyResult = await CopyApplicationFilesAsync(logger);
                if (!copyResult)
                {
                    logger?.LogError("File copy failed during service installation");
                    return false;
                }

                // Install the service using sc command
                var installCommand = $"create \"{ServiceName}\" binPath= \"{Path.Combine(InstallFolder, "mcp_nexus.exe")} --service\" start= auto DisplayName= \"{DisplayName}\"";
                var installResult = await RunScCommandAsync(installCommand, logger);
                
                if (installResult)
                {
                    logger?.LogInformation("Service installed successfully");
                    return true;
                }
                else
                {
                    logger?.LogError("Service installation failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception during service installation");
                return false;
            }
        }

        /// <summary>
        /// Uninstalls the Windows service asynchronously
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public static async Task<bool> UninstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Uninstalling MCP Nexus service");

                // Use force delete to handle "marked for deletion" state
                var deleteResult = await ForceDeleteServiceAsync(logger);
                
                if (deleteResult)
                {
                    logger?.LogInformation("Service uninstalled successfully");
                    return true;
                }
                else
                {
                    logger?.LogError("Service uninstallation failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception during service uninstallation");
                return false;
            }
        }


        /// <summary>
        /// Updates the Windows service asynchronously
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public static async Task<bool> UpdateServiceAsync(ILogger? logger = null)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("                    MCP NEXUS SERVICE UPDATE");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();

                logger?.LogInformation("Starting MCP Nexus service update");

                // Step 0: Check if service exists
                Console.WriteLine("Checking if service exists...");
                logger?.LogInformation("Checking if MCP-Nexus service exists");

                var serviceExists = await CheckServiceExistsAsync(logger);
                if (!serviceExists)
                {
                    Console.WriteLine("⚠ Service does not exist. Installing service first...");
                    logger?.LogInformation("Service does not exist, installing first");
                    
                    var installResult = await InstallServiceAsync(logger);
                    if (!installResult)
                    {
                        Console.WriteLine("✗ Service installation failed");
                        logger?.LogError("Service installation failed during update");
                        return false;
                    }
                    Console.WriteLine("✓ Service installed successfully");
                    return true;
                }

                // Step 1: Stop the service if it's running
                Console.WriteLine("Stopping MCP-Nexus service...");
                logger?.LogInformation("Stopping MCP-Nexus service");

                var stopResult = await RunScCommandWithForceAsync("stop \"MCP-Nexus\"", logger!, false);
                if (stopResult)
                {
                    Console.WriteLine("✓ Service stopped successfully");
                    await Task.Delay(2000); // Wait for service to fully stop
                }
                else
                {
                    Console.WriteLine("⚠ Service stop command failed (may not be running)");
                }

                // Step 2: Build the project
                Console.WriteLine("Building project...");
                logger?.LogInformation("Building project for deployment");

                var buildResult = await BuildProjectForDeploymentAsync(logger);
                if (!buildResult)
                {
                    Console.WriteLine("✗ Build failed");
                    logger?.LogError("Project build failed during update");
                    return false;
                }
                Console.WriteLine("✓ Build completed successfully");

                // Step 3: Copy new files to installation directory
                Console.WriteLine("Deploying new files...");
                logger?.LogInformation("Deploying new files to installation directory");

                var deployResult = await CopyApplicationFilesAsync(logger);
                if (!deployResult)
                {
                    Console.WriteLine("✗ File deployment failed");
                    logger?.LogError("File deployment failed during update");
                    return false;
                }
                Console.WriteLine("✓ Files deployed successfully");

                // Step 4: Start the service
                Console.WriteLine("Starting updated service...");
                logger?.LogInformation("Starting updated MCP-Nexus service");

                var startResult = await RunScCommandWithForceAsync("start \"MCP-Nexus\"", logger!, false);
                if (startResult)
                {
                    Console.WriteLine("✓ Service started successfully");
                    await Task.Delay(3000); // Wait for service to fully start
                }
                else
                {
                    Console.WriteLine("⚠ Service start failed - you may need to start it manually");
                    logger?.LogWarning("Service start failed after update");
                }

                Console.WriteLine();
                Console.WriteLine("🎉 Service update completed successfully!");
                logger?.LogInformation("MCP Nexus service update completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Update failed: {ex.Message}");
                logger?.LogError(ex, "Exception during service update");
                return false;
            }
        }





        /// <summary>
        /// Checks if a Windows service exists asynchronously
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <returns>True if the service exists, false otherwise</returns>
        private static async Task<bool> CheckServiceExistsAsync(ILogger? logger = null)
        {
            try
            {
                logger?.LogDebug("Checking if service '{ServiceName}' exists", ServiceName);

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"query \"{ServiceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    logger?.LogError("Failed to start SC process for service check");
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Service exists if exit code is 0 and output contains service state information
                var serviceExists = process.ExitCode == 0 && output.Contains("SERVICE_NAME:");
                
                logger?.LogDebug("Service '{ServiceName}' exists: {Exists}", ServiceName, serviceExists);
                return serviceExists;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception checking if service '{ServiceName}' exists", ServiceName);
                return false;
            }
        }

        /// <summary>
        /// Public method to forcefully remove a service that may be marked for deletion.
        /// This method handles the "marked for deletion" state by waiting and retrying.
        /// </summary>
        public static async Task<bool> ForceUninstallServiceAsync(ILogger? logger = null)
        {
            return await ForceDeleteServiceAsync(logger);
        }

        /// <summary>
        /// Forcefully removes a service that may be marked for deletion.
        /// This method handles the "marked for deletion" state by waiting and retrying.
        /// </summary>
        private static async Task<bool> ForceDeleteServiceAsync(ILogger? logger = null)
        {
            const int maxRetries = 10;
            const int retryDelayMs = 2000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                logger?.LogDebug("Force delete attempt {Attempt}/{MaxRetries} for service '{ServiceName}'", 
                    attempt, maxRetries, ServiceName);

                // First, try to stop the service if it's running
                var stopCommand = $"stop \"{ServiceName}\"";
                await RunScCommandAsync(stopCommand, logger);

                // Wait a moment for the service to stop
                await Task.Delay(1000);

                // Try to delete the service
                var deleteCommand = $"delete \"{ServiceName}\"";
                var deleteResult = await RunScCommandAsync(deleteCommand, logger);

                if (deleteResult)
                {
                    logger?.LogInformation("Service '{ServiceName}' deleted successfully on attempt {Attempt}", 
                        ServiceName, attempt);
                    return true;
                }

                // Check if the service still exists
                var stillExists = await CheckServiceExistsAsync(logger);
                if (!stillExists)
                {
                    logger?.LogInformation("Service '{ServiceName}' no longer exists after attempt {Attempt}", 
                        ServiceName, attempt);
                    return true;
                }

                logger?.LogWarning("Service '{ServiceName}' still exists after delete attempt {Attempt}, waiting {DelayMs}ms before retry", 
                    ServiceName, attempt, retryDelayMs);

                // Wait before retrying
                await Task.Delay(retryDelayMs);
            }

            logger?.LogError("Failed to delete service '{ServiceName}' after {MaxRetries} attempts", 
                ServiceName, maxRetries);
            return false;
        }

        /// <summary>
        /// Runs a Windows Service Control (sc) command with force option asynchronously.
        /// </summary>
        /// <param name="command">The sc command to execute.</param>
        /// <param name="logger">The logger instance for recording command operations and errors.</param>
        /// <param name="force">Whether to force the command execution.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the command executed successfully; otherwise, <c>false</c>.
        /// </returns>
        private static async Task<bool> RunScCommandWithForceAsync(string command, ILogger logger, bool force)
        {
            // Add force parameter to the command if needed
            var fullCommand = force ? $"{command} /force" : command;
            return await RunScCommandAsync(fullCommand, logger);
        }


        // Constants expected by tests
        private const string ServiceName = "MCP-Nexus";
        public const string DisplayName = "MCP Nexus Service";
        public const string Description = "MCP Nexus Debugging Service";

        // Additional constants expected by tests
        private const string ServiceDisplayName = "MCP Nexus Server";
        private const string ServiceDescription = "Model Context Protocol server providing AI tool integration";
        private const string InstallFolder = "C:\\Program Files\\MCP-Nexus";

        /// <summary>
        /// Runs a Windows Service Control (sc) command asynchronously.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the sc command.</param>
        /// <param name="logger">The logger instance for recording command operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the command executed successfully; otherwise, <c>false</c>.
        /// </returns>
        private static async Task<bool> RunScCommandAsync(string arguments, ILogger? logger = null)
        {
            try
            {
                logger?.LogDebug("Running SC command: sc {Arguments}", arguments);

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    logger?.LogError("Failed to start SC process");
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    logger?.LogDebug("SC command succeeded: {Output}", output.Trim());
                    return true;
                }
                else
                {
                    logger?.LogError("SC command failed with exit code {ExitCode}. Output: {Output}. Error: {Error}",
                        process.ExitCode, output.Trim(), error.Trim());
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception running SC command '{Arguments}'", arguments);
                return false;
            }
        }

        /// <summary>
        /// Builds the project for deployment asynchronously using dotnet build command.
        /// </summary>
        /// <param name="logger">The logger instance for recording build operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the build completed successfully; otherwise, <c>false</c>.
        /// </returns>
        private static async Task<bool> BuildProjectForDeploymentAsync(ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Building project for deployment");

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "build --configuration Release --output bin/Release/net8.0/publish",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    logger?.LogError("Failed to start dotnet build process");
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    logger?.LogInformation("Build completed successfully");
                    return true;
                }
                else
                {
                    logger?.LogError("Build failed with exit code {ExitCode}. Output: {Output}. Error: {Error}",
                        process.ExitCode, output.Trim(), error.Trim());
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception during project build");
                return false;
            }
        }

        /// <summary>
        /// Copies application files to the installation directory asynchronously.
        /// </summary>
        /// <param name="logger">The logger instance for recording copy operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the copy operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        private static async Task<bool> CopyApplicationFilesAsync(ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Copying application files to installation directory");

                var sourceDir = Path.Combine(Environment.CurrentDirectory, "bin", "Release", "net8.0", "publish");
                var targetDir = InstallFolder;

                if (!Directory.Exists(sourceDir))
                {
                    logger?.LogError("Source directory does not exist: {SourceDir}", sourceDir);
                    return false;
                }

                // Create target directory if it doesn't exist
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    logger?.LogInformation("Created installation directory: {TargetDir}", targetDir);
                }

                // Copy all files from source to target
                var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(sourceDir, file);
                    var targetFile = Path.Combine(targetDir, relativePath);
                    var targetFileDir = Path.GetDirectoryName(targetFile);

                    if (!string.IsNullOrEmpty(targetFileDir) && !Directory.Exists(targetFileDir))
                    {
                        Directory.CreateDirectory(targetFileDir);
                    }

                    await Task.Run(() => File.Copy(file, targetFile, true));
                    logger?.LogDebug("Copied file: {RelativePath}", relativePath);
                }

                logger?.LogInformation("Successfully copied {FileCount} files to installation directory", files.Length);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception during file copy operation");
                return false;
            }
        }
    }
}
