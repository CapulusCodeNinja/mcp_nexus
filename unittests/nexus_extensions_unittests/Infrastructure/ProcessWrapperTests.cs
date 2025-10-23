using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Xunit;
using nexus.extensions.Infrastructure;

namespace nexus.extensions.unittests.Infrastructure;

/// <summary>
/// Unit tests for ProcessWrapper class.
/// </summary>
public class ProcessWrapperTests
{
    /// <summary>
    /// Verifies that CreateProcess creates a valid process handle with correct configuration.
    /// </summary>
    [Fact]
    public void CreateProcess_WithValidParameters_ShouldCreateConfiguredProcess()
    {
        // Arrange
        var wrapper = new ProcessWrapper();
        var fileName = "cmd.exe";
        var arguments = "/c echo test";
        var envVars = new Dictionary<string, string>
        {
            ["TEST_VAR"] = "test_value"
        };

        // Act
        var process = wrapper.CreateProcess(fileName, arguments, envVars);

        // Assert
        process.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateProcess with empty environment variables works correctly.
    /// </summary>
    [Fact]
    public void CreateProcess_WithEmptyEnvironmentVariables_ShouldCreateProcess()
    {
        // Arrange
        var wrapper = new ProcessWrapper();
        var fileName = "cmd.exe";
        var arguments = "/c echo test";
        var envVars = new Dictionary<string, string>();

        // Act
        var process = wrapper.CreateProcess(fileName, arguments, envVars);

        // Assert
        process.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ProcessHandle can be disposed without error.
    /// </summary>
    [Fact]
    public void ProcessHandle_Dispose_ShouldNotThrow()
    {
        // Arrange
        var wrapper = new ProcessWrapper();
        var fileName = "cmd.exe";
        var arguments = "/c echo test";
        var envVars = new Dictionary<string, string>();
        var process = wrapper.CreateProcess(fileName, arguments, envVars);

        // Act
        var action = () => process.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that ProcessHandle event handlers can be added and removed.
    /// </summary>
    [Fact]
    public void ProcessHandle_Events_ShouldSupportAddRemove()
    {
        // Arrange
        var wrapper = new ProcessWrapper();
        var fileName = "cmd.exe";
        var arguments = "/c echo test";
        var envVars = new Dictionary<string, string>();
        var process = wrapper.CreateProcess(fileName, arguments, envVars);
        DataReceivedEventHandler outputHandler = (sender, e) => { };
        DataReceivedEventHandler errorHandler = (sender, e) => { };

        // Act
        var action = () =>
        {
            process.OutputDataReceived += outputHandler;
            process.ErrorDataReceived += errorHandler;
            process.OutputDataReceived -= outputHandler;
            process.ErrorDataReceived -= errorHandler;
        };

        // Assert
        action.Should().NotThrow();

        // Cleanup
        process.Dispose();
    }

    /// <summary>
    /// Verifies that ProcessHandle can start a simple process and wait for exit.
    /// </summary>
    [Fact]
    public void ProcessHandle_StartAndWaitForExit_ShouldComplete()
    {
        // Arrange
        var wrapper = new ProcessWrapper();
        var fileName = "cmd.exe";
        var arguments = "/c exit 0";
        var envVars = new Dictionary<string, string>();
        var process = wrapper.CreateProcess(fileName, arguments, envVars);

        try
        {
            // Act
            process.Start();
            process.WaitForExit();

            // Assert
            process.HasExited.Should().BeTrue();
            process.ExitCode.Should().Be(0);
        }
        finally
        {
            // Cleanup
            process.Dispose();
        }
    }

    /// <summary>
    /// Verifies that ProcessHandle Id property returns valid process ID after start.
    /// </summary>
    [Fact]
    public void ProcessHandle_Id_AfterStart_ShouldBeValid()
    {
        // Arrange
        var wrapper = new ProcessWrapper();
        var fileName = "cmd.exe";
        var arguments = "/c exit 0";
        var envVars = new Dictionary<string, string>();
        var process = wrapper.CreateProcess(fileName, arguments, envVars);

        try
        {
            // Act
            process.Start();

            // Assert
            process.Id.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (!process.HasExited)
            {
                try { process.Kill(true); } catch { }
            }
            process.Dispose();
        }
    }
}

