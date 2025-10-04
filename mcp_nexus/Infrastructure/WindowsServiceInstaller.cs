using System.Runtime.Versioning;
using System.ServiceProcess;
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
        // Constants - MUST be at the top of the class definition
        private const string m_ServiceName = "MCP-Nexus";
        public const string m_DisplayName = "MCP Nexus Service";
        public const string m_Description = "MCP Nexus Debugging Service";
        private const string m_ServiceDisplayName = "MCP Nexus Server";
        private const string m_ServiceDescription = "Model Context Protocol server providing AI tool integration";
        private const string m_InstallFolder = "C:\\Program Files\\MCP-Nexus";

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
                var installCommand = $"create \"{m_ServiceName}\" binPath= \"{Path.Combine(m_InstallFolder, "mcp_nexus.exe")} --service\" start= auto DisplayName= \"{m_DisplayName}\"";
                var installResult = await RunScCommandAsync(installCommand, logger);
                
                if (installResult)
                {
                    logger?.LogInformation("Service installed successfully");
                    
                // Start the service using ServiceController API
                var startResult = await StartServiceAsync(logger);
                
                if (startResult)
                {
                    logger?.LogInformation("Service started successfully");
                }
                else
                {
                    logger?.LogWarning("Service installed but failed to start");
                }
                    
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

                // Stop the service first using ServiceController API
                var stopResult = await StopServiceAsync(logger);
                if (stopResult)
                {
                    logger?.LogInformation("Service stopped successfully");
                    await Task.Delay(2000); // Wait for service to fully stop
                }
                else
                {
                    logger?.LogWarning("Service stop failed (may not be running)");
                }

                // Delete the service (with retry for "marked for deletion" state)
                var deleteCommand = $"delete \"{m_ServiceName}\"";
                var deleteResult = await RunScCommandAsync(deleteCommand, logger);
                
                if (deleteResult)
                {
                    logger?.LogInformation("Service uninstalled successfully");
                    return true;
                }
                else
                {
                    // If delete failed, wait a bit and try again (handles "marked for deletion" state)
                    logger?.LogWarning("Service delete failed, waiting and retrying...");
                    await Task.Delay(3000);
                    
                    var retryDeleteResult = await RunScCommandAsync(deleteCommand, logger);
                    if (retryDeleteResult)
                    {
                        logger?.LogInformation("Service uninstalled successfully on retry");
                        return true;
                    }
                    else
                    {
                        logger?.LogError("Service uninstallation failed even after retry");
                        return false;
                    }
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

                // Step 1: Check if service is running and stop it if needed
                Console.WriteLine("Checking if MCP-Nexus service is running...");
                logger?.LogInformation("Checking if MCP-Nexus service is running");
                
                var (exists, isRunning) = GetServiceStatus();
                bool wasRunning = false;
                
                if (exists)
                {
                    if (isRunning)
                    {
                        Console.WriteLine("Service is running, stopping it...");
                        logger?.LogInformation("Service is running, stopping it");
                        
                        var stopResult = await StopServiceAsync(logger);
                        if (stopResult)
                        {
                            Console.WriteLine("✓ Service stopped successfully");
                            wasRunning = true;
                            await Task.Delay(2000); // Wait for service to fully stop
                        }
                        else
                        {
                            Console.WriteLine("⚠ Service stop failed");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Service is not running");
                        logger?.LogInformation("Service is not running");
                    }
                }
                else
                {
                    Console.WriteLine("Service does not exist");
                    logger?.LogInformation("Service does not exist");
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

                // Step 4: Start the service only if it was running before the update
                if (wasRunning)
                {
                    Console.WriteLine("Starting updated service...");
                    logger?.LogInformation("Starting updated MCP-Nexus service");

                    var startResult = await StartServiceAsync(logger);
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
                }
                else
                {
                    Console.WriteLine("Service was not running before update, leaving it stopped");
                    logger?.LogInformation("Service was not running before update, leaving it stopped");
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
        private static Task<bool> CheckServiceExistsAsync(ILogger? logger = null)
        {
            try
            {
                logger?.LogDebug("Checking if service '{ServiceName}' exists", m_ServiceName);

                var (exists, _) = GetServiceStatus();
                
                logger?.LogDebug("Service '{ServiceName}' exists: {Exists}", m_ServiceName, exists);
                return Task.FromResult(exists);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception checking if service '{ServiceName}' exists", m_ServiceName);
                return Task.FromResult(false);
            }
        }





        /// <summary>
        /// Check if the service exists and is running
        /// </summary>
        private static (bool exists, bool isRunning) GetServiceStatus()
        {
            try
            {
                using var service = new ServiceController(m_ServiceName);
                var status = service.Status;
                return (true, status == ServiceControllerStatus.Running);
            }
            catch (InvalidOperationException)
            {
                // Service doesn't exist
                return (false, false);
            }
        }

        /// <summary>
        /// Start the service using ServiceController API
        /// </summary>
        private static Task<bool> StartServiceAsync(ILogger? logger = null)
        {
            try
            {
                using var service = new ServiceController(m_ServiceName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                logger?.LogInformation("Service is already running");
                return Task.FromResult(true);
            }

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            
            logger?.LogInformation("Service started successfully");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to start service");
            return Task.FromResult(false);
        }
        }

        /// <summary>
        /// Stop the service using ServiceController API
        /// </summary>
        private static Task<bool> StopServiceAsync(ILogger? logger = null)
        {
            try
            {
                using var service = new ServiceController(m_ServiceName);
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                logger?.LogInformation("Service is already stopped");
                return Task.FromResult(true);
            }

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            
            logger?.LogInformation("Service stopped successfully");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to stop service");
            return Task.FromResult(false);
        }
        }


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
                var targetDir = m_InstallFolder;

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
