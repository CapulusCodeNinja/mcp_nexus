using System.Runtime.Versioning;
using System.ServiceProcess;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Setup.Interfaces;
using Nexus.Setup.Models;

using NLog;

namespace Nexus.Setup.Core;

/// <summary>
/// Implements Windows service installation.
/// </summary>
[SupportedOSPlatform("windows")]
internal class ServiceInstaller : IServiceInstaller
{
    private readonly Logger m_Logger;
    private readonly IFileSystem m_FileSystem;
    private readonly IProcessManager m_ProcessManager;
    private readonly IServiceController m_ServiceController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstaller"/> class.
    /// </summary>
    public ServiceInstaller()
        : this(new FileSystem(), new ProcessManager(), new ServiceControllerWrapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstaller"/> class.
    /// </summary>
    /// <param name="fileSystem">File system abstraction.</param>
    /// <param name="processManager">Process manager abstraction.</param>
    /// <param name="serviceController">Service controller abstraction.</param>
    internal ServiceInstaller(
        IFileSystem fileSystem,
        IProcessManager processManager,
        IServiceController serviceController)
    {
        m_Logger = LogManager.GetCurrentClassLogger();

        m_FileSystem = fileSystem;
        m_ProcessManager = processManager;
        m_ServiceController = serviceController;
    }

    /// <summary>
    /// Installs a Windows service.
    /// </summary>
    /// <param name="options">Installation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installation result.</returns>
    public async Task<ServiceInstallationResult> InstallServiceAsync(ServiceInstallationOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new ArgumentException("Service name cannot be null or empty.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ExecutablePath))
        {
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(options));
        }

        if (!m_FileSystem.FileExists(options.ExecutablePath))
        {
            return ServiceInstallationResult.CreateFailure(options.ServiceName, "Executable file not found", options.ExecutablePath);
        }

        m_Logger.Info("Installing service: {ServiceName}", options.ServiceName);

        if (m_ServiceController.IsServiceInstalled(options.ServiceName))
        {
            m_Logger.Warn("Service {ServiceName} is already installed", options.ServiceName);
            return ServiceInstallationResult.CreateFailure(options.ServiceName, "Service is already installed");
        }

        try
        {
            var startMode = options.StartMode switch
            {
                Models.ServiceStartMode.Automatic => "auto",
                Models.ServiceStartMode.Manual => "demand",
                Models.ServiceStartMode.Disabled => "disabled",
                _ => "auto",
            };

            var accountName = GetAccountName(options.Account, options.AccountUsername);
            var arguments = $"create \"{options.ServiceName}\" binPath= \"\\\"{options.ExecutablePath}\\\" --service\" start= {startMode} DisplayName= \"{options.DisplayName}\"";

            if (options.Account != ServiceAccount.LocalSystem)
            {
                arguments += $" obj= \"{accountName}\"";
                if (options.Account == ServiceAccount.Custom && !string.IsNullOrEmpty(options.AccountPassword))
                {
                    arguments += $" password= \"{options.AccountPassword}\"";
                }
            }

            var (success, output, errorDetails) = await ExecuteScCommandAsync(arguments, cancellationToken);

            if (!success)
            {
                m_Logger.Error("Failed to install service: {Error}", errorDetails);
                return ServiceInstallationResult.CreateFailure(options.ServiceName, "Failed to install service", errorDetails);
            }

            // Set service description
            if (!string.IsNullOrEmpty(options.Description))
            {
                var descArgs = $"description \"{options.ServiceName}\" \"{options.Description}\"";
                _ = await ExecuteScCommandAsync(descArgs, cancellationToken);
            }

            m_Logger.Info("Service {ServiceName} installed successfully", options.ServiceName);
            return ServiceInstallationResult.CreateSuccess(options.ServiceName, "Service installed successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Exception while installing service {ServiceName}", options.ServiceName);
            return ServiceInstallationResult.CreateFailure(options.ServiceName, "Exception during installation", ex.Message);
        }
    }

    /// <summary>
    /// Uninstalls a Windows service.
    /// </summary>
    /// <param name="serviceName">The name of the service to uninstall.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The uninstallation result.</returns>
    public async Task<ServiceInstallationResult> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));
        }

        m_Logger.Info("Uninstalling service: {ServiceName}", serviceName);

        if (!m_ServiceController.IsServiceInstalled(serviceName))
        {
            m_Logger.Warn("Service {ServiceName} is not installed", serviceName);
            return ServiceInstallationResult.CreateFailure(serviceName, "Service is not installed");
        }

        try
        {
            // Stop the service if it's running
            var status = m_ServiceController.GetServiceStatus(serviceName);
            if (status == null)
            {
                m_Logger.Info("Service {ServiceName} is not installed - nothing to stop", serviceName);
            }
            else if (status == ServiceControllerStatus.Stopped)
            {
                m_Logger.Info("Service {ServiceName} is already stopped", serviceName);
            }
            else
            {
                // Service is running or in transitional state - try to stop it
                try
                {
                    m_Logger.Info("Stopping service {ServiceName}...", serviceName);
                    m_ServiceController.StopService(serviceName);
                    m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    m_Logger.Info("Service {ServiceName} stopped successfully", serviceName);
                }
                catch (Exception ex)
                {
                    m_Logger.Warn(ex, "Failed to stop service {ServiceName}, but continuing with uninstall", serviceName);
                }
            }

            // Delete the service
            var arguments = $"delete \"{serviceName}\"";
            var (success, output, errorDetails) = await ExecuteScCommandAsync(arguments, cancellationToken);

            if (!success)
            {
                m_Logger.Error("Failed to uninstall service: {Error}", errorDetails);
                return ServiceInstallationResult.CreateFailure(serviceName, "Failed to uninstall service", errorDetails);
            }

            m_Logger.Info("Service {ServiceName} uninstalled successfully", serviceName);
            return ServiceInstallationResult.CreateSuccess(serviceName, "Service uninstalled successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Exception while uninstalling service {ServiceName}", serviceName);
            return ServiceInstallationResult.CreateFailure(serviceName, "Exception during uninstallation", ex.Message);
        }
    }

    /// <summary>
    /// Checks if a service is installed.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>True if the service is installed, false otherwise.</returns>
    public bool IsServiceInstalled(string serviceName)
    {
        return !string.IsNullOrWhiteSpace(serviceName) && m_ServiceController.IsServiceInstalled(serviceName);
    }

    /// <summary>
    /// Gets the status of a service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The service status, or null if the service doesn't exist.</returns>
    public ServiceControllerStatus? GetServiceStatus(string serviceName)
    {
        return string.IsNullOrWhiteSpace(serviceName) ? null : m_ServiceController.GetServiceStatus(serviceName);
    }

    /// <summary>
    /// Waits for a service to reach a specific status.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="targetStatus">The target status to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service reached the target status, false if timeout occurred.</returns>
    public async Task<bool> WaitForServiceStatusAsync(string serviceName, string targetStatus, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));
        }

        if (string.IsNullOrWhiteSpace(targetStatus))
        {
            throw new ArgumentException("Target status cannot be null or empty.", nameof(targetStatus));
        }

        var startTime = DateTime.Now;
        var pollInterval = TimeSpan.FromMilliseconds(100);

        m_Logger.Debug("Waiting for service {ServiceName} to reach status {TargetStatus}", serviceName, targetStatus);

        while (DateTime.Now - startTime < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                m_Logger.Info("Wait for service status cancelled");
                return false;
            }

            try
            {
                var currentStatus = m_ServiceController.GetServiceStatus(serviceName);

                if (currentStatus != null && currentStatus.Value.ToString().Equals(targetStatus, StringComparison.OrdinalIgnoreCase))
                {
                    m_Logger.Debug("Service {ServiceName} reached target status {Status}", serviceName, targetStatus);
                    return true;
                }

                m_Logger.Debug(
                    "Service {ServiceName} status: {CurrentStatus}, waiting for {TargetStatus}",
                    serviceName,
                    currentStatus,
                    targetStatus);
            }
            catch (Exception ex)
            {
                m_Logger.Debug(ex, "Error checking service {ServiceName} status", serviceName);
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        m_Logger.Warn(
            "Service {ServiceName} did not reach status {Status} within {Timeout}ms",
            serviceName,
            targetStatus,
            timeout.TotalMilliseconds);
        return false;
    }

    /// <summary>
    /// Builds the project for deployment.
    /// </summary>
    /// <param name="projectPath">Path to the project file or directory.</param>
    /// <param name="configuration">Build configuration (e.g., "Release", "Debug").</param>
    /// <param name="outputPath">Output path for build artifacts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the build succeeded, false otherwise.</returns>
    public async Task<bool> BuildProjectAsync(string projectPath, string configuration = "Release", string? outputPath = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be null or empty.", nameof(projectPath));
        }

        if (string.IsNullOrWhiteSpace(configuration))
        {
            throw new ArgumentException("Configuration cannot be null or empty.", nameof(configuration));
        }

        try
        {
            m_Logger.Info("Building project for deployment: {ProjectPath}", projectPath);

            var workingDirectory = m_FileSystem.DirectoryExists(projectPath)
                ? projectPath
                : m_FileSystem.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;

            var arguments = $"build --configuration {configuration}";
            if (!string.IsNullOrEmpty(outputPath))
            {
                arguments += $" --output \"{outputPath}\"";
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };

            using var process = m_ProcessManager.StartProcess(startInfo);

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                m_Logger.Info("Build completed successfully");
                m_Logger.Debug("Build output: {Output}", output);
                return true;
            }
            else
            {
                m_Logger.Error(
                    "Build failed with exit code {ExitCode}. Output: {Output}. Error: {Error}",
                    process.ExitCode,
                    output,
                    error);
                return false;
            }
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Exception during project build");
            return false;
        }
    }

    /// <summary>
    /// Copies application files from source to installation directory.
    /// </summary>
    /// <param name="sourceDirectory">Source directory containing the application files.</param>
    /// <param name="targetDirectory">Target installation directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the copy operation succeeded, false otherwise.</returns>
    public async Task<bool> CopyApplicationFilesAsync(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            throw new ArgumentException("Source directory cannot be null or empty.", nameof(sourceDirectory));
        }

        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new ArgumentException("Target directory cannot be null or empty.", nameof(targetDirectory));
        }

        try
        {
            m_Logger.Info("Copying application files from {Source} to {Target}", sourceDirectory, targetDirectory);

            if (!m_FileSystem.DirectoryExists(sourceDirectory))
            {
                m_Logger.Error("Source directory does not exist: {SourceDir}", sourceDirectory);
                return false;
            }

            // Create target directory if it doesn't exist
            if (!m_FileSystem.DirectoryExists(targetDirectory))
            {
                m_FileSystem.CreateDirectory(targetDirectory);
                m_Logger.Debug("Created installation directory: {TargetDir}", targetDirectory);
            }

            // Copy all files from source to target
            var files = m_FileSystem.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            var copiedCount = 0;

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    m_Logger.Info("File copy operation cancelled");
                    return false;
                }

                var relativePath = Path.GetRelativePath(sourceDirectory, file);
                var targetFile = m_FileSystem.CombinePaths(targetDirectory, relativePath);
                var targetFileDir = m_FileSystem.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetFileDir) && !m_FileSystem.DirectoryExists(targetFileDir))
                {
                    m_FileSystem.CreateDirectory(targetFileDir);
                }

                await m_FileSystem.CopyFileAsync(file, targetFile, overwrite: true, cancellationToken);
                copiedCount++;
                m_Logger.Debug("Copied file: {RelativePath}", relativePath);
            }

            m_Logger.Info("Successfully copied {FileCount} files to installation directory", copiedCount);
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Exception during file copy operation");
            return false;
        }
    }

    /// <summary>
    /// Gets the account name for a service account type.
    /// </summary>
    /// <param name="account">The service account type.</param>
    /// <param name="customUsername">Custom username if using custom account.</param>
    /// <returns>The account name.</returns>
    private static string GetAccountName(ServiceAccount account, string? customUsername)
    {
        return account switch
        {
            ServiceAccount.LocalSystem => "LocalSystem",
            ServiceAccount.LocalService => "NT AUTHORITY\\LocalService",
            ServiceAccount.NetworkService => "NT AUTHORITY\\NetworkService",
            ServiceAccount.Custom => customUsername ?? throw new ArgumentException("Custom username must be provided for custom account"),
            _ => "LocalSystem",
        };
    }

    /// <summary>
    /// Executes an sc.exe command.
    /// </summary>
    /// <param name="arguments">Command arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    private async Task<(bool Success, string Output, string? ErrorDetails)> ExecuteScCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = m_ProcessManager.StartProcess(startInfo);

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            var success = process.ExitCode == 0;
            m_Logger.Debug("sc.exe exit code: {ExitCode}, Output: {Output}", process.ExitCode, output);

            return (success, output, success ? null : error);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Exception executing sc.exe command");
            return (false, string.Empty, ex.Message);
        }
    }
}
