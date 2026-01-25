using WinAiDbg.Setup.Models;

using Xunit;

namespace WinAiDbg.Setup_unittests.Models;

/// <summary>
/// Unit tests for ServiceInstallationResult.
/// </summary>
public class ServiceInstallationResultTests
{
    /// <summary>
    /// Verifies CreateSuccess creates a successful result.
    /// </summary>
    [Fact]
    public void CreateSuccess_ReturnsSuccessResult()
    {
        // Act
        var result = ServiceInstallationResult.CreateSuccess("TestService", "Success message");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("TestService", result.ServiceName);
        Assert.Equal("Success message", result.Message);
        Assert.Null(result.ErrorDetails);
    }

    /// <summary>
    /// Verifies CreateFailure creates a failure result.
    /// </summary>
    [Fact]
    public void CreateFailure_ReturnsFailureResult()
    {
        // Act
        var result = ServiceInstallationResult.CreateFailure("TestService", "Error message", "Error details");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("TestService", result.ServiceName);
        Assert.Equal("Error message", result.Message);
        Assert.Equal("Error details", result.ErrorDetails);
    }

    /// <summary>
    /// Verifies CreateFailure with null error details.
    /// </summary>
    [Fact]
    public void CreateFailure_WithNullErrorDetails_ReturnsFailureResult()
    {
        // Act
        var result = ServiceInstallationResult.CreateFailure("TestService", "Error message");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("TestService", result.ServiceName);
        Assert.Equal("Error message", result.Message);
        Assert.Null(result.ErrorDetails);
    }
}
