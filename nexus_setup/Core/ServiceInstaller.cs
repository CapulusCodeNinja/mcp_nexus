using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using nexus.setup.Models;

namespace nexus.setup.Core;

/// <summary>
/// Implements Windows service installation.
/// </summary>
[SupportedOSPlatform("windows")]
internal class ServiceInstaller : IServiceInstaller
{
    private readonly ILogger<ServiceInstaller> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstaller"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ServiceInstaller(ILogger<ServiceInstaller> logger)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            throw new ArgumentException("Service name cannot be null or empty.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.ExecutablePath))
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(options));

        if (!File.Exists(options.ExecutablePath))
            return ServiceInstallationResult.CreateFailure(options.ServiceName, "Executable file not found", options.ExecutablePath);

        m_Logger.LogInformation("Installing service: {ServiceName}", options.ServiceName);

        if (IsServiceInstalled(options.ServiceName))
        {
            m_Logger.LogWarning("Service {ServiceName} is already installed", options.ServiceName);
            return ServiceInstallationResult.CreateFailure(options.ServiceName, "Service is already installed");
        }

        try
        {
            var startMode = options.StartMode switch
            {
                Models.ServiceStartMode.Automatic => "auto",
                Models.ServiceStartMode.Manual => "demand",
                Models.ServiceStartMode.Disabled => "disabled",
                _ => "auto"
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

            var result = await ExecuteScCommandAsync(arguments, cancellationToken);

            if (!result.Success)
            {
                m_Logger.LogError("Failed to install service: {Error}", result.ErrorDetails);
                return ServiceInstallationResult.CreateFailure(options.ServiceName, "Failed to install service", result.ErrorDetails);
            }

            // Set service description
            if (!string.IsNullOrEmpty(options.Description))
            {
                var descArgs = $"description \"{options.ServiceName}\" \"{options.Description}\"";
                await ExecuteScCommandAsync(descArgs, cancellationToken);
            }

            m_Logger.LogInformation("Service {ServiceName} installed successfully", options.ServiceName);
            return ServiceInstallationResult.CreateSuccess(options.ServiceName, "Service installed successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Exception while installing service {ServiceName}", options.ServiceName);
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
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));

        m_Logger.LogInformation("Uninstalling service: {ServiceName}", serviceName);

        if (!IsServiceInstalled(serviceName))
        {
            m_Logger.LogWarning("Service {ServiceName} is not installed", serviceName);
            return ServiceInstallationResult.CreateFailure(serviceName, "Service is not installed");
        }

        try
        {
            // Stop the service if it's running
            using (var controller = new ServiceController(serviceName))
            {
                if (controller.Status != ServiceControllerStatus.Stopped)
                {
                    m_Logger.LogInformation("Stopping service {ServiceName}...", serviceName);
                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }

            // Delete the service
            var arguments = $"delete \"{serviceName}\"";
            var result = await ExecuteScCommandAsync(arguments, cancellationToken);

            if (!result.Success)
            {
                m_Logger.LogError("Failed to uninstall service: {Error}", result.ErrorDetails);
                return ServiceInstallationResult.CreateFailure(serviceName, "Failed to uninstall service", result.ErrorDetails);
            }

            m_Logger.LogInformation("Service {ServiceName} uninstalled successfully", serviceName);
            return ServiceInstallationResult.CreateSuccess(serviceName, "Service uninstalled successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Exception while uninstalling service {ServiceName}", serviceName);
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
        if (string.IsNullOrWhiteSpace(serviceName))
            return false;

        try
        {
            using var controller = new ServiceController(serviceName);
            var _ = controller.Status; // This will throw if service doesn't exist
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the status of a service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The service status, or null if the service doesn't exist.</returns>
    public string? GetServiceStatus(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return null;

        try
        {
            using var controller = new ServiceController(serviceName);
            return controller.Status.ToString();
        }
        catch
        {
            return null;
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
            _ => "LocalSystem"
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
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            var success = process.ExitCode == 0;
            m_Logger.LogDebug("sc.exe exit code: {ExitCode}, Output: {Output}", process.ExitCode, output);

            return (success, output, success ? null : error);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Exception executing sc.exe command");
            return (false, string.Empty, ex.Message);
        }
    }
}

