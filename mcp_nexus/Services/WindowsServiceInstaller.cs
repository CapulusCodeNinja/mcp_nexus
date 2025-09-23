using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace mcp_nexus.Services
{
    [SupportedOSPlatform("windows")]
    public static class WindowsServiceInstaller
    {
        private const string ServiceName = "MCP-Nexus";
        private const string ServiceDisplayName = "MCP Nexus Server";
        private const string ServiceDescription = "Model Context Protocol server providing AI tool integration";
        private const string InstallFolder = @"C:\Program Files\MCP-Nexus";


        public static async Task<bool> InstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                if (!IsRunAsAdministrator())
                {
                    var errorMsg = "Installation requires administrator privileges. Please run the command as administrator.";
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "{ErrorMsg}", errorMsg);
                    await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Installing MCP Nexus as Windows service");

                // Check if service already exists
                if (IsServiceInstalled())
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service already installed. Uninstalling first");
                    await UninstallServiceAsync(logger); // Continue even if uninstall has issues

                    // Wait and check again
                    await Task.Delay(2000);

                    if (IsServiceInstalled())
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, "Service still exists after uninstall attempt. This may be normal if it's marked for deletion");
                    }
                }

                // Create installation directory
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Creating installation directory: {InstallFolder}", InstallFolder);
                if (Directory.Exists(InstallFolder))
                {
                    Directory.Delete(InstallFolder, true);
                }
                Directory.CreateDirectory(InstallFolder);

                // Build the project in Release mode for deployment
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Building project in Release mode for deployment");
                if (!await BuildProjectForDeploymentAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Failed to build project for deployment");
                    return false;
                }

                // Copy application files
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Copying application files");
                await CopyApplicationFilesAsync(logger);

                // Install the service (with retry logic for "marked for deletion")
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Registering Windows service");
                var executablePath = Path.Combine(InstallFolder, "mcp_nexus.exe");
                var arguments = "--service";

                var installCommand = $@"create ""{ServiceName}"" binPath= ""{executablePath} {arguments}"" " +
                                   $@"start= auto DisplayName= ""{ServiceDisplayName}""";

                var result = await RunScCommandAsync(installCommand, logger);
                if (!result)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, "Service creation failed. Attempting to clear 'marked for deletion' state");

                    // Try to force cleanup the service registration
                    if (await ForceCleanupServiceAsync(logger))
                    {
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service cleanup successful. Retrying installation");
                        result = await RunScCommandAsync(installCommand, logger);
                    }

                    if (!result)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, "Trying alternative service cleanup methods");

                        // Alternative method: Try to start and then delete again
                        await RunScCommandAsync($@"start ""{ServiceName}""", logger, allowFailure: true);
                        await Task.Delay(2000);
                        await RunScCommandAsync($@"stop ""{ServiceName}""", logger, allowFailure: true);
                        await Task.Delay(2000);
                        await RunScCommandAsync($@"delete ""{ServiceName}""", logger, allowFailure: true);
                        await Task.Delay(3000);

                        // Try one more time
                        result = await RunScCommandAsync(installCommand, logger);
                    }

                    if (!result)
                    {
                        OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Failed to install service after all cleanup attempts");
                        var errorMsg = "Manual cleanup options:\n" +
                                     "1. Try: dotnet run -- --force-uninstall\n" +
                                     "2. Or run these commands as admin:\n" +
                                     "   sc delete \"MCP-Nexus\"\n" +
                                     "   reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\MCP-Nexus\" /f\n" +
                                     "3. Then retry installation";
                        OperationLogger.LogError(logger, OperationLogger.Operations.Install, "{ErrorMsg}", errorMsg);
                        await Console.Error.WriteLineAsync(errorMsg);
                        return false;
                    }
                }

                // Set service description
                await RunScCommandAsync($@"description ""{ServiceName}"" ""{ServiceDescription}""", logger);

                // Start the service
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Starting MCP Nexus service");
                await RunScCommandAsync($@"start ""{ServiceName}""", logger);

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "MCP Nexus service installed and started successfully");
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service Name: {ServiceName}", ServiceName);
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Install Path: {InstallFolder}", InstallFolder);
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "HTTP Endpoint: http://localhost:5000/mcp");

                Console.WriteLine("✅ MCP Nexus service installed and started successfully!");
                Console.WriteLine($"   Service Name: {ServiceName}");
                Console.WriteLine($"   Install Path: {InstallFolder}");
                Console.WriteLine("   HTTP Endpoint: http://localhost:5000/mcp");
                Console.WriteLine();
                Console.WriteLine("Use 'dotnet run -- --uninstall' to remove the service.");

                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, ex, "Installation failed");
                await Console.Error.WriteLineAsync($"Installation failed: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> UninstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                if (!IsRunAsAdministrator())
                {
                    var errorMsg = "Uninstallation requires administrator privileges. Please run the command as administrator.";
                    OperationLogger.LogError(logger, OperationLogger.Operations.Uninstall, "{ErrorMsg}", errorMsg);
                    await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Uninstalling MCP Nexus service");

                if (!IsServiceInstalled())
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Service is not installed");
                    return true;
                }

                // Stop the service (ignore errors if service is not running)
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Stopping service");
                var stopResult = await RunScCommandAsync($@"stop ""{ServiceName}""", logger, allowFailure: true);
                if (stopResult)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Service stopped successfully");
                    await Task.Delay(3000); // Give it time to stop
                }
                else
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Service was not running or already stopped");
                    await Task.Delay(1000); // Brief delay
                }

                // Delete the service (retry if marked for deletion)
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Removing service registration");
                var deleteResult = await RunScCommandAsync($@"delete ""{ServiceName}""", logger, allowFailure: true);

                if (!deleteResult)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, "Service might be marked for deletion. Waiting and retrying");
                    await Task.Delay(5000); // Wait longer

                    // Try again
                    deleteResult = await RunScCommandAsync($@"delete ""{ServiceName}""", logger, allowFailure: true);

                    if (!deleteResult)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, "Could not remove service registration. Continuing with installation");
                        // Don't fail the entire process - continue with installation
                    }
                }

                // Remove installation directory
                if (Directory.Exists(InstallFolder))
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Removing installation files");
                    await Task.Delay(1000); // Give the service time to fully stop

                    try
                    {
                        Directory.Delete(InstallFolder, true);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Removed installation directory: {InstallFolder}", InstallFolder);
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, "Could not remove installation directory");
                        Console.WriteLine($"Warning: Could not remove installation directory: {ex.Message}");
                        Console.WriteLine("You may need to remove it manually after reboot.");
                    }
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "MCP Nexus service uninstalled successfully");
                Console.WriteLine("✅ MCP Nexus service uninstalled successfully!");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Uninstall, ex, "Uninstallation failed");
                await Console.Error.WriteLineAsync($"Uninstallation failed: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ForceUninstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                if (!IsRunAsAdministrator())
                {
                    var errorMsg = "Force uninstallation requires administrator privileges. Please run the command as administrator.";
                    OperationLogger.LogError(logger, OperationLogger.Operations.ForceUninstall, "{ErrorMsg}", errorMsg);
                    await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, "Force uninstalling MCP Nexus service");
                OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, "This will remove all service registrations and registry entries");

                // Use the force cleanup method
                var unused = await ForceCleanupServiceAsync(logger);

                // Remove installation directory regardless
                if (Directory.Exists(InstallFolder))
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Removing installation files");
                    try
                    {
                        Directory.Delete(InstallFolder, true);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Removed installation directory: {InstallFolder}", InstallFolder);
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, "Could not remove installation directory");
                        Console.WriteLine($"Warning: Could not remove installation directory: {ex.Message}");
                    }
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, "Force uninstall completed");
                Console.WriteLine("✅ Force uninstall completed!");
                Console.WriteLine("The service should now be completely removed from the system.");

                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.ForceUninstall, ex, "Force uninstallation failed");
                await Console.Error.WriteLineAsync($"Force uninstallation failed: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> BuildProjectForDeploymentAsync(ILogger? logger = null)
        {
            try
            {
                // Find the project directory (go up from the current executable location)
                var currentDir = AppContext.BaseDirectory;
                var projectDir = FindProjectDirectory(currentDir);

                if (string.IsNullOrEmpty(projectDir))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, "Could not find project directory containing .csproj file");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Build, "Found project directory: {ProjectDir}", projectDir);

                // Build the project in Release mode
                var buildProcess = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "build --configuration Release",
                    WorkingDirectory = projectDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(buildProcess);
                if (process == null)
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, "Failed to start dotnet build process");
                    return false;
                }

                await process.WaitForExitAsync();

                var unused = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode != 0)
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, "Build failed (exit code {ExitCode}): {Error}", process.ExitCode, error);
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Build, "Project built successfully for deployment");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Build, ex, "Failed to build project");
                return false;
            }
        }

        private static string? FindProjectDirectory(string startPath)
        {
            var dir = new DirectoryInfo(startPath);

            // Go up the directory tree looking for a .csproj file
            while (dir != null)
            {
                if (dir.GetFiles("*.csproj").Any())
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }

        private static async Task CopyApplicationFilesAsync(ILogger? logger = null)
        {
            var buildOutputDirectory = AppContext.BaseDirectory;
            OperationLogger.LogDebug(logger, OperationLogger.Operations.Copy, "Copying files from build output: {BuildOutputDirectory}", buildOutputDirectory);

            // Get all files from the build output directory
            var sourceFiles = Directory.GetFiles(buildOutputDirectory, "*", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) && // Skip debug symbols
                           !Path.GetFileName(f).EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)) // Skip deps file
                .ToList();

            OperationLogger.LogDebug(logger, OperationLogger.Operations.Copy, "Found {FileCount} files to copy from {BuildOutputDirectory}", sourceFiles.Count, buildOutputDirectory);

            foreach (var sourceFile in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                var targetFile = Path.Combine(InstallFolder, fileName);

                OperationLogger.LogDebug(logger, OperationLogger.Operations.Copy, "Copying: {FileName}", fileName);
                await Task.Run(() => File.Copy(sourceFile, targetFile, true));
            }

            // Also copy any subdirectories that might contain runtime files
            var subDirectories = Directory.GetDirectories(buildOutputDirectory, "*", SearchOption.TopDirectoryOnly);
            foreach (var subDir in subDirectories)
            {
                var dirName = Path.GetFileName(subDir);
                // Skip common development directories
                if (dirName.Equals("ref", StringComparison.OrdinalIgnoreCase))
                    continue;

                var targetSubDir = Path.Combine(InstallFolder, dirName);
                await CopyDirectoryAsync(subDir, targetSubDir);
            }

            OperationLogger.LogInfo(logger, OperationLogger.Operations.Copy, "Copied application files to installation directory");
        }

        private static async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            // Copy files
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var targetFile = Path.Combine(targetDir, relativePath);
                var targetFileDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetFileDir))
                {
                    Directory.CreateDirectory(targetFileDir);
                }

                await Task.Run(() => File.Copy(file, targetFile, true));
            }
        }

        private static async Task<bool> ForceCleanupServiceAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Attempting to force cleanup service registration");

                // Method 1: Try to query the service and get its state
                var queryResult = await RunScCommandAsync($@"query ""{ServiceName}""", logger, allowFailure: true);
                if (queryResult)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Service is still visible. Attempting forced deletion");
                }

                // Method 2: Comprehensive PowerShell cleanup
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Using PowerShell for comprehensive service cleanup");
                var psCommand = $@"
                    try {{
                        # Stop any running service first
                        $service = Get-Service -Name '{ServiceName}' -ErrorAction SilentlyContinue;
                        if ($service) {{
                            if ($service.Status -eq 'Running') {{
                                Stop-Service -Name '{ServiceName}' -Force -ErrorAction SilentlyContinue;
                                Write-Host 'Service stopped';
                            }}
                        }}

                        # Try WMI deletion
                        $wmiService = Get-WmiObject -Class Win32_Service -Filter ""Name='{ServiceName}'"" -ErrorAction SilentlyContinue;
                        if ($wmiService) {{
                            $wmiService.Delete();
                            Write-Host 'Service deleted via WMI';
                        }}
                        
                        # Force registry cleanup - multiple locations
                        $regPaths = @(
                            'HKLM:\SYSTEM\CurrentControlSet\Services\{ServiceName}',
                            'HKLM:\SYSTEM\ControlSet001\Services\{ServiceName}',
                            'HKLM:\SYSTEM\ControlSet002\Services\{ServiceName}'
                        );
                        
                        foreach ($regPath in $regPaths) {{
                            if (Test-Path $regPath) {{
                                Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue;
                                Write-Host ""Removed registry: $regPath"";
                            }}
                        }}
                        
                        # Additional cleanup - service control manager cache
                        $scmPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\ServiceCurrent';
                        if (Test-Path $scmPath) {{
                            Get-ItemProperty -Path $scmPath -ErrorAction SilentlyContinue | Where-Object {{ $_.PSChildName -eq '{ServiceName}' }} | Remove-Item -Force -ErrorAction SilentlyContinue;
                        }}
                        
                        # Force refresh service control manager
                        [System.GC]::Collect();
                        [System.GC]::WaitForPendingFinalizers();
                        
                        Write-Host 'Comprehensive cleanup completed';
                    }} catch {{
                        Write-Host ""PowerShell cleanup error: $($_.Exception.Message)"";
                    }}
                ";

                var psProcess = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{psCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psProcess);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        OperationLogger.LogDebug(logger, OperationLogger.Operations.Cleanup, "PowerShell output: {Output}", output.Trim());
                    }
                }

                // Wait a bit for cleanup to take effect
                await Task.Delay(5000);

                // Method 3: Direct registry manipulation if PowerShell didn't work
                var finalCheck = await RunScCommandAsync($@"query ""{ServiceName}""", logger, allowFailure: true);
                if (finalCheck)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Service still exists. Trying direct registry manipulation");
                    if (await DirectRegistryCleanupAsync(logger))
                    {
                        await Task.Delay(3000);
                        finalCheck = await RunScCommandAsync($@"query ""{ServiceName}""", logger, allowFailure: true);
                    }
                }

                if (!finalCheck)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Service successfully cleaned up");
                    return true;
                }
                else
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Cleanup, "Service cleanup may not be complete");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Cleanup, ex, "Force cleanup failed");
                return false;
            }
        }

        private static async Task<bool> DirectRegistryCleanupAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Registry, "Attempting direct registry cleanup");

                // Direct registry access for service cleanup
                var serviceKeys = new[]
                {
                    $@"SYSTEM\CurrentControlSet\Services\{ServiceName}",
                    $@"SYSTEM\ControlSet001\Services\{ServiceName}",
                    $@"SYSTEM\ControlSet002\Services\{ServiceName}"
                };

                var deletedAny = false;
                foreach (var keyPath in serviceKeys)
                {
                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
                        if (key != null)
                        {
                            Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
                            OperationLogger.LogInfo(logger, OperationLogger.Operations.Registry, "Deleted registry key: HKLM\\{KeyPath}", keyPath);
                            deletedAny = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Registry, "Could not delete registry key {KeyPath}: {Error}", keyPath, ex.Message);
                    }
                }

                // Try to refresh the service control manager
                if (deletedAny)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Registry, "Registry entries deleted. Refreshing service control manager");

                    // Try to restart the service control manager to refresh the cache
                    var refreshProcess = new ProcessStartInfo
                    {
                        FileName = "net.exe",
                        Arguments = "stop scm",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    try
                    {
                        using var process = Process.Start(refreshProcess);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                        }
                    }
                    catch
                    {
                        // Ignore errors - this is a best-effort attempt
                    }

                    await Task.Delay(2000);

                    refreshProcess.Arguments = "start scm";
                    try
                    {
                        using var process = Process.Start(refreshProcess);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                        }
                    }
                    catch
                    {
                        // Ignore errors - this is a best-effort attempt
                    }
                }

                return deletedAny;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Registry, ex, "Direct registry cleanup failed");
                return false;
            }
        }

        private static async Task<bool> RunScCommandAsync(string arguments, ILogger? logger = null, bool allowFailure = false)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    if (!allowFailure)
                        OperationLogger.LogError(logger, OperationLogger.Operations.Service, "Failed to start sc.exe process");
                    return false;
                }

                await process.WaitForExitAsync();

                var unused = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode != 0)
                {
                    if (!allowFailure)
                    {
                        OperationLogger.LogError(logger, OperationLogger.Operations.Service, "SC command failed (exit code {ExitCode}): sc {Arguments}", process.ExitCode, arguments);
                        if (!string.IsNullOrEmpty(error))
                            OperationLogger.LogError(logger, OperationLogger.Operations.Service, "SC command error: {Error}", error);
                    }
                    else
                    {
                        OperationLogger.LogDebug(logger, OperationLogger.Operations.Service, "SC command returned exit code {ExitCode} (expected for: sc {Arguments})", process.ExitCode, arguments);
                    }
                    return false;
                }

                OperationLogger.LogDebug(logger, OperationLogger.Operations.Service, "SC command successful: sc {Arguments}", arguments);
                return true;
            }
            catch (Exception ex)
            {
                if (!allowFailure)
                    OperationLogger.LogError(logger, OperationLogger.Operations.Service, ex, "Failed to execute SC command: {Arguments}", arguments);
                return false;
            }
        }

        private static bool IsServiceInstalled()
        {
            try
            {
                using var service = ServiceController.GetServices()
                    .FirstOrDefault(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase));
                return service != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRunAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> UpdateServiceAsync(ILogger? logger = null)
        {
            try
            {
                if (!IsRunAsAdministrator())
                {
                    var errorMsg = "Service update requires administrator privileges. Please run the command as administrator.";
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "{ErrorMsg}", errorMsg);
                    await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Starting MCP Nexus service update");
                Console.WriteLine("🔄 Starting MCP Nexus service update...");

                // Check if service exists
                if (!IsServiceInstalled())
                {
                    var errorMsg = "MCP Nexus service is not installed. Use --install to install it first.";
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "{ErrorMsg}", errorMsg);
                    await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
                    return false;
                }

                // Step 1: Stop the service
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Stopping MCP Nexus service for update");
                Console.WriteLine("📱 Stopping MCP Nexus service...");
                var stopResult = await RunScCommandAsync($@"stop ""{ServiceName}""", logger, allowFailure: true);
                if (stopResult)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service stopped successfully");
                    Console.WriteLine("✅ Service stopped successfully");
                }
                else
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service was not running or already stopped");
                    Console.WriteLine("ℹ️ Service was not running");
                }

                // Wait for service to fully stop
                await Task.Delay(3000);

                // Step 2: Build the project in Release mode
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Building project for deployment");
                Console.WriteLine("🔨 Building project in Release mode...");
                if (!await BuildProjectForDeploymentAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Failed to build project for update");
                    await Console.Error.WriteLineAsync("❌ Failed to build project for update");
                    return false;
                }

                // Step 3: Update files
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Updating application files");
                Console.WriteLine("📁 Updating application files...");

                // Create backup of current installation
                var backupsBaseFolder = Path.Combine(InstallFolder, "backups");
                var backupFolder = Path.Combine(backupsBaseFolder, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Creating backup: {BackupFolder}", backupFolder);
                try
                {
                    if (Directory.Exists(InstallFolder))
                    {
                        // Create backups directory if it doesn't exist
                        if (!Directory.Exists(backupsBaseFolder))
                        {
                            Directory.CreateDirectory(backupsBaseFolder);
                        }

                        // Create the specific backup folder
                        Directory.CreateDirectory(backupFolder);

                        // Copy all files except the backups folder itself
                        var sourceFiles = Directory.GetFiles(InstallFolder, "*", SearchOption.TopDirectoryOnly);
                        foreach (var sourceFile in sourceFiles)
                        {
                            var fileName = Path.GetFileName(sourceFile);
                            var targetFile = Path.Combine(backupFolder, fileName);
                            File.Copy(sourceFile, targetFile, true);
                        }

                        // Copy subdirectories except backups
                        var sourceDirectories = Directory.GetDirectories(InstallFolder)
                            .Where(dir => !string.Equals(Path.GetFileName(dir), "backups", StringComparison.OrdinalIgnoreCase))
                            .ToArray();

                        foreach (var sourceDir in sourceDirectories)
                        {
                            var dirName = Path.GetFileName(sourceDir);
                            var targetDir = Path.Combine(backupFolder, dirName);
                            await CopyDirectoryAsync(sourceDir, targetDir);
                        }

                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Backup created successfully");
                        Console.WriteLine($"💾 Backup created: {backupFolder}");
                    }
                }
                catch (Exception ex)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Update, "Could not create backup: {Error}", ex.Message);
                    Console.WriteLine($"⚠️ Warning: Could not create backup: {ex.Message}");
                }

                // Copy new files
                await CopyApplicationFilesAsync(logger);
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Application files updated successfully");
                Console.WriteLine("✅ Application files updated");

                // Step 4: Start the service
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Starting MCP Nexus service");
                Console.WriteLine("🚀 Starting MCP Nexus service...");
                var startResult = await RunScCommandAsync($@"start ""{ServiceName}""", logger);
                if (startResult)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service started successfully");
                    Console.WriteLine("✅ Service started successfully");
                }
                else
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Failed to start service after update");
                    await Console.Error.WriteLineAsync("❌ Failed to start service after update");
                    await Console.Error.WriteLineAsync($"You can manually start it with: sc start \"{ServiceName}\"");
                    await Console.Error.WriteLineAsync($"Or restore from backup: {backupFolder}");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "MCP Nexus service updated successfully");
                Console.WriteLine();
                Console.WriteLine("✅ MCP Nexus service updated successfully!");
                Console.WriteLine($"   Service Name: {ServiceName}");
                Console.WriteLine($"   Install Path: {InstallFolder}");
                Console.WriteLine("   HTTP Endpoint: http://localhost:5000/mcp");
                Console.WriteLine($"   Backup Location: {backupFolder}");
                Console.WriteLine();

                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, "Service update failed");
                await Console.Error.WriteLineAsync($"❌ Service update failed: {ex.Message}");
                return false;
            }
        }
    }
}










