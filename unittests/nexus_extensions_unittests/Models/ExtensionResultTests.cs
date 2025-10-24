using FluentAssertions;

using nexus.extensions.Models;

using Xunit;

namespace nexus.extensions_unittests.Models;

/// <summary>
/// Unit tests for the ExtensionResult class.
/// </summary>
public class ExtensionResultTests
{
    /// <summary>
    /// Verifies that ExtensionResult can be instantiated with default values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new ExtensionResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Output.Should().Be(string.Empty);
        result.Error.Should().BeNull();
        result.ExitCode.Should().Be(0);
        result.ExecutionTime.Should().Be(TimeSpan.Zero);
        result.StandardError.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Success property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Success_ShouldSetAndGetValue()
    {
        // Arrange
        var result = new ExtensionResult
        {
            // Act
            Success = true
        };

        // Assert
        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Output property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Output_ShouldSetAndGetValue()
    {
        // Arrange
        var result = new ExtensionResult();
        const string output = "{\"status\": \"completed\"}";

        // Act
        result.Output = output;

        // Assert
        result.Output.Should().Be(output);
    }

    /// <summary>
    /// Verifies that Error property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Error_ShouldSetAndGetValue()
    {
        // Arrange
        var result = new ExtensionResult();
        const string error = "Extension failed to execute";

        // Act
        result.Error = error;

        // Assert
        result.Error.Should().Be(error);
    }

    /// <summary>
    /// Verifies that Error property can be null.
    /// </summary>
    [Fact]
    public void Error_ShouldAllowNullValue()
    {
        // Arrange
        var result = new ExtensionResult
        {
            Error = "Some error"
        };

        // Act
        result.Error = null;

        // Assert
        result.Error.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ExitCode property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ExitCode_ShouldSetAndGetValue()
    {
        // Arrange
        var result = new ExtensionResult();
        const int exitCode = 1;

        // Act
        result.ExitCode = exitCode;

        // Assert
        result.ExitCode.Should().Be(exitCode);
    }

    /// <summary>
    /// Verifies that ExecutionTime property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ExecutionTime_ShouldSetAndGetValue()
    {
        // Arrange
        var result = new ExtensionResult();
        var executionTime = TimeSpan.FromSeconds(5);

        // Act
        result.ExecutionTime = executionTime;

        // Assert
        result.ExecutionTime.Should().Be(executionTime);
    }

    /// <summary>
    /// Verifies that StandardError property can be set and retrieved.
    /// </summary>
    [Fact]
    public void StandardError_ShouldSetAndGetValue()
    {
        // Arrange
        var result = new ExtensionResult();
        const string standardError = "Error output from process";

        // Act
        result.StandardError = standardError;

        // Assert
        result.StandardError.Should().Be(standardError);
    }

    /// <summary>
    /// Verifies that StandardError property can be null.
    /// </summary>
    [Fact]
    public void StandardError_ShouldAllowNullValue()
    {
        // Arrange
        var result = new ExtensionResult
        {
            StandardError = "Some error"
        };

        // Act
        result.StandardError = null;

        // Assert
        result.StandardError.Should().BeNull();
    }

    /// <summary>
    /// Verifies that all properties can be set via object initializer for success scenario.
    /// </summary>
    [Fact]
    public void ObjectInitializer_ShouldSetAllPropertiesForSuccess()
    {
        // Act
        var result = new ExtensionResult
        {
            Success = true,
            Output = "{\"result\": \"success\"}",
            Error = null,
            ExitCode = 0,
            ExecutionTime = TimeSpan.FromSeconds(2.5),
            StandardError = null
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("{\"result\": \"success\"}");
        result.Error.Should().BeNull();
        result.ExitCode.Should().Be(0);
        result.ExecutionTime.Should().Be(TimeSpan.FromSeconds(2.5));
        result.StandardError.Should().BeNull();
    }

    /// <summary>
    /// Verifies that all properties can be set via object initializer for failure scenario.
    /// </summary>
    [Fact]
    public void ObjectInitializer_ShouldSetAllPropertiesForFailure()
    {
        // Act
        var result = new ExtensionResult
        {
            Success = false,
            Output = string.Empty,
            Error = "Extension execution failed",
            ExitCode = 1,
            ExecutionTime = TimeSpan.FromSeconds(1.2),
            StandardError = "Process error details"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Output.Should().Be(string.Empty);
        result.Error.Should().Be("Extension execution failed");
        result.ExitCode.Should().Be(1);
        result.ExecutionTime.Should().Be(TimeSpan.FromSeconds(1.2));
        result.StandardError.Should().Be("Process error details");
    }
}

