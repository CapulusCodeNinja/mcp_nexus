namespace Nexus.Setup.Models;

/// <summary>
/// Result of a service installation or update operation.
/// </summary>
public class ServiceInstallationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error details (if operation failed).
    /// </summary>
    public string? ErrorDetails
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A successful result.</returns>
    public static ServiceInstallationResult CreateSuccess(string serviceName, string message)
    {
        return new ServiceInstallationResult
        {
            Success = true,
            ServiceName = serviceName,
            Message = message,
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="message">The error message.</param>
    /// <param name="errorDetails">Detailed error information.</param>
    /// <returns>A failure result.</returns>
    public static ServiceInstallationResult CreateFailure(string serviceName, string message, string? errorDetails = null)
    {
        return new ServiceInstallationResult
        {
            Success = false,
            ServiceName = serviceName,
            Message = message,
            ErrorDetails = errorDetails,
        };
    }
}
