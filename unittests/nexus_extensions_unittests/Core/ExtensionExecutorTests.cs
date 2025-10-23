using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using nexus.extensions.Configuration;
using nexus.extensions.Core;
using nexus.extensions.Infrastructure;
using nexus.extensions.Models;

namespace nexus.extensions.unittests.Core;

/// <summary>
/// Unit tests for ExtensionExecutor class.
/// </summary>
public class ExtensionExecutorTests : IDisposable
{
    private readonly Mock<IExtensionManager> m_MockExtensionManager;
    private readonly Mock<IProcessWrapper> m_MockProcessWrapper;
    private readonly Mock<IExtensionTokenValidator> m_MockTokenValidator;
    private readonly ExtensionConfiguration m_Configuration;
    private readonly ILogger<ExtensionExecutor> m_Logger;
    private ExtensionExecutor? m_Executor;

    /// <summary>
    /// Initializes a new instance of the ExtensionExecutorTests class.
    /// </summary>
    public ExtensionExecutorTests()
    {
        m_MockExtensionManager = new Mock<IExtensionManager>();
        m_MockProcessWrapper = new Mock<IProcessWrapper>();
        m_MockTokenValidator = new Mock<IExtensionTokenValidator>();
        m_Configuration = new ExtensionConfiguration
        {
            Enabled = true,
            ExtensionsPath = "extensions",
            CallbackPort = 5555
        };
        m_Logger = NullLogger<ExtensionExecutor>.Instance;
    }

    /// <summary>
    /// Verifies that constructor with valid parameters creates an instance.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Assert
        m_Executor.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that constructor with null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ExtensionExecutor(
            null!,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that constructor with null extension manager throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullExtensionManager_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ExtensionExecutor(
            m_Logger,
            null!,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("extensionManager");
    }

    /// <summary>
    /// Verifies that constructor with null callback URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullCallbackUrl_ShouldThrowArgumentException()
    {
        // Act
        var action = () => new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            null!,
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithParameterName("callbackUrl");
    }

    /// <summary>
    /// Verifies that constructor with empty callback URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyCallbackUrl_ShouldThrowArgumentException()
    {
        // Act
        var action = () => new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithParameterName("callbackUrl");
    }

    /// <summary>
    /// Verifies that constructor with null configuration throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            null!,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// Verifies that constructor with null token validator throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTokenValidator_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            null!,
            m_MockProcessWrapper.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("tokenValidator");
    }

    /// <summary>
    /// Verifies that constructor with null process wrapper creates a default ProcessWrapper.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProcessWrapper_ShouldCreateDefaultWrapper()
    {
        // Act
        var action = () => m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            null); // Null process wrapper should use default

        // Assert
        action.Should().NotThrow();
        m_Executor.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ExecuteAsync with empty extension name throws ArgumentException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithEmptyExtensionName_ShouldThrowArgumentException()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var action = async () => await m_Executor.ExecuteAsync(
            "",
            "session-1",
            null,
            "cmd-1");

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("extensionName");
    }

    /// <summary>
    /// Verifies that ExecuteAsync with empty session ID throws ArgumentException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithEmptySessionId_ShouldThrowArgumentException()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var action = async () => await m_Executor.ExecuteAsync(
            "test-extension",
            "",
            null,
            "cmd-1");

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sessionId");
    }

    /// <summary>
    /// Verifies that ExecuteAsync with empty command ID throws ArgumentException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithEmptyCommandId_ShouldThrowArgumentException()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var action = async () => await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "");

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("commandId");
    }

    /// <summary>
    /// Verifies that ExecuteAsync with non-existent extension throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNonExistentExtension_ShouldThrowInvalidOperationException()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        m_MockExtensionManager.Setup(m => m.GetExtension("non-existent"))
            .Returns((ExtensionMetadata?)null);

        // Act
        var action = async () => await m_Executor.ExecuteAsync(
            "non-existent",
            "session-1",
            null,
            "cmd-1");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-existent*");
    }

    /// <summary>
    /// Verifies that ExecuteAsync with invalid extension throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithInvalidExtension_ShouldThrowInvalidOperationException()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 5000
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);

        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((false, "Validation failed"));

        // Act
        var action = async () => await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*validation failed*");
    }

    /// <summary>
    /// Verifies that KillExtension with empty command ID returns false.
    /// </summary>
    [Fact]
    public void KillExtension_WithEmptyCommandId_ShouldReturnFalse()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var result = m_Executor.KillExtension("");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that KillExtension with non-existent command ID returns false.
    /// </summary>
    [Fact]
    public void KillExtension_WithNonExistentCommandId_ShouldReturnFalse()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var result = m_Executor.KillExtension("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GetExtensionInfo with empty command ID returns null.
    /// </summary>
    [Fact]
    public void GetExtensionInfo_WithEmptyCommandId_ShouldReturnNull()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var result = m_Executor.GetExtensionInfo("");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetExtensionInfo with non-existent command ID returns null.
    /// </summary>
    [Fact]
    public void GetExtensionInfo_WithNonExistentCommandId_ShouldReturnNull()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var result = m_Executor.GetExtensionInfo("non-existent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times without error.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        // Act
        var action = () =>
        {
            m_Executor.Dispose();
            m_Executor.Dispose();
            m_Executor.Dispose();
        };

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that ExecuteAsync with successful extension returns success result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulExtension_ShouldReturnSuccessResult()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 5000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess = new Mock<IProcessHandle>();
        mockProcess.Setup(p => p.Id).Returns(12345);
        mockProcess.Setup(p => p.HasExited).Returns(false).Callback(() =>
        {
            mockProcess.Setup(p => p.HasExited).Returns(true);
        });
        mockProcess.Setup(p => p.ExitCode).Returns(0);
        mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockProcess.Setup(p => p.WaitForExit()).Callback(() => { });

        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Returns(mockProcess.Object);

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Act
        var result = await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(0);
    }

    /// <summary>
    /// Verifies that ExecuteAsync with process failure returns failure result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithProcessFailure_ShouldReturnFailureResult()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 5000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess = new Mock<IProcessHandle>();
        mockProcess.Setup(p => p.Id).Returns(12345);
        mockProcess.Setup(p => p.HasExited).Returns(false).Callback(() =>
        {
            mockProcess.Setup(p => p.HasExited).Returns(true);
        });
        mockProcess.Setup(p => p.ExitCode).Returns(1);
        mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockProcess.Setup(p => p.WaitForExit()).Callback(() => { });

        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Returns(mockProcess.Object);

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Act
        var result = await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(1);
    }

    /// <summary>
    /// Verifies that ExecuteAsync with process start failure returns failure result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithProcessStartFailure_ShouldReturnFailureResult()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 5000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess = new Mock<IProcessHandle>();
        mockProcess.Setup(p => p.Start())
            .Throws(new InvalidOperationException("Failed to start process"));

        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Returns(mockProcess.Object);

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Act
        var result = await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Failed to start process");
    }

    /// <summary>
    /// Verifies that ExecuteAsync with unsupported script type returns failure.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithUnsupportedScriptType_ShouldReturnFailureResult()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.py",
            ScriptType = "python", // Unsupported
            Timeout = 5000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Act
        var result = await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unsupported script type");
    }

    /// <summary>
    /// Verifies that ExecuteAsync revokes token after completion.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AfterCompletion_ShouldRevokeToken()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 5000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess = new Mock<IProcessHandle>();
        mockProcess.Setup(p => p.Id).Returns(12345);
        mockProcess.Setup(p => p.HasExited).Returns(false).Callback(() =>
        {
            mockProcess.Setup(p => p.HasExited).Returns(true);
        });
        mockProcess.Setup(p => p.ExitCode).Returns(0);
        mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockProcess.Setup(p => p.WaitForExit()).Callback(() => { });

        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Returns(mockProcess.Object);

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Act
        await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Assert
        m_MockTokenValidator.Verify(t => t.RevokeToken("test-token"), Times.Once);
    }

    /// <summary>
    /// Verifies that GetExtensionInfo returns info for running extension.
    /// </summary>
    [Fact]
    public async Task GetExtensionInfo_WithRunningExtension_ShouldReturnInfo()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 30000, // Long timeout
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess = new Mock<IProcessHandle>();
        var tcs = new TaskCompletionSource();
        mockProcess.Setup(p => p.Id).Returns(12345);
        mockProcess.Setup(p => p.HasExited).Returns(false);
        mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task); // Never complete, simulating long-running process

        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Returns(mockProcess.Object);

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Start execution but don't await
        var executeTask = m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Wait a bit for the process to start
        await Task.Delay(50);

        // Act
        var info = m_Executor.GetExtensionInfo("cmd-1");

        // Assert
        info.Should().NotBeNull();
        info!.CommandId.Should().Be("cmd-1");
        info.ExtensionName.Should().Be("test-extension");
        info.SessionId.Should().Be("session-1");
        info.IsRunning.Should().BeTrue();

        // Cleanup - complete the task
        tcs.SetResult();
        try { await executeTask; } catch { }
    }

    /// <summary>
    /// Verifies that KillExtension kills running extension.
    /// </summary>
    [Fact]
    public async Task KillExtension_WithRunningExtension_ShouldKillProcess()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 30000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess = new Mock<IProcessHandle>();
        var tcs = new TaskCompletionSource();
        mockProcess.Setup(p => p.Id).Returns(12345);
        mockProcess.Setup(p => p.HasExited).Returns(false);
        mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);
        mockProcess.Setup(p => p.Kill(true)).Callback(() => tcs.SetCanceled());

        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Returns(mockProcess.Object);

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Start execution but don't await
        var executeTask = m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            null,
            "cmd-1");

        // Wait for process to start
        await Task.Delay(50);

        // Act
        var result = m_Executor.KillExtension("cmd-1");

        // Assert
        result.Should().BeTrue();
        mockProcess.Verify(p => p.Kill(true), Times.AtLeastOnce);

        // Cleanup
        try { await executeTask; } catch { }
    }

    /// <summary>
    /// Verifies that ExecuteAsync with parameters passes them to process.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithParameters_ShouldPassToProcess()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 5000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess = new Mock<IProcessHandle>();
        mockProcess.Setup(p => p.Id).Returns(12345);
        mockProcess.Setup(p => p.HasExited).Returns(false).Callback(() =>
        {
            mockProcess.Setup(p => p.HasExited).Returns(true);
        });
        mockProcess.Setup(p => p.ExitCode).Returns(0);
        mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockProcess.Setup(p => p.WaitForExit()).Callback(() => { });

        string? capturedArguments = null;
        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Callback<string, string, Dictionary<string, string>>((file, args, env) =>
            {
                capturedArguments = args;
            })
            .Returns(mockProcess.Object);

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        var parameters = new
        {
            threadId = "1234",
            verbose = true
        };

        // Act
        await m_Executor.ExecuteAsync(
            "test-extension",
            "session-1",
            parameters,
            "cmd-1");

        // Assert
        capturedArguments.Should().NotBeNull();
        capturedArguments.Should().Contain("-ThreadId");
        capturedArguments.Should().Contain("-Verbose");
    }

    /// <summary>
    /// Verifies that Dispose kills all running extensions.
    /// </summary>
    [Fact]
    public async Task Dispose_WithRunningExtensions_ShouldKillAll()
    {
        // Arrange
        m_Executor = new ExtensionExecutor(
            m_Logger,
            m_MockExtensionManager.Object,
            "http://localhost:5555",
            m_Configuration,
            m_MockTokenValidator.Object,
            m_MockProcessWrapper.Object);

        var metadata = new ExtensionMetadata
        {
            Name = "test-extension",
            Version = "1.0.0",
            Description = "Test",
            ScriptFile = "test.ps1",
            ScriptType = "powershell",
            Timeout = 30000,
            ExtensionPath = "C:\\extensions\\test"
        };

        m_MockExtensionManager.Setup(m => m.GetExtension("test-extension"))
            .Returns(metadata);
        m_MockExtensionManager.Setup(m => m.ValidateExtension("test-extension"))
            .Returns((true, string.Empty));

        var mockProcess1 = new Mock<IProcessHandle>();
        var mockProcess2 = new Mock<IProcessHandle>();
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        mockProcess1.Setup(p => p.Id).Returns(12345);
        mockProcess1.Setup(p => p.HasExited).Returns(false);
        mockProcess1.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs1.Task);
        mockProcess1.Setup(p => p.Kill(true)).Callback(() => tcs1.SetCanceled());

        mockProcess2.Setup(p => p.Id).Returns(12346);
        mockProcess2.Setup(p => p.HasExited).Returns(false);
        mockProcess2.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs2.Task);
        mockProcess2.Setup(p => p.Kill(true)).Callback(() => tcs2.SetCanceled());

        var processQueue = new Queue<IProcessHandle>(new[] { mockProcess1.Object, mockProcess2.Object });
        m_MockProcessWrapper.Setup(w => w.CreateProcess(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .Returns(() => processQueue.Dequeue());

        m_MockTokenValidator.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-token");

        // Start two extensions
        var task1 = m_Executor.ExecuteAsync("test-extension", "session-1", null, "cmd-1");
        var task2 = m_Executor.ExecuteAsync("test-extension", "session-1", null, "cmd-2");
        await Task.Delay(50); // Let them start

        // Act
        m_Executor.Dispose();

        // Assert
        mockProcess1.Verify(p => p.Kill(true), Times.AtLeastOnce);
        mockProcess2.Verify(p => p.Kill(true), Times.AtLeastOnce);

        // Cleanup
        try { await task1; } catch { }
        try { await task2; } catch { }
    }

    /// <summary>
    /// Cleanup after each test.
    /// </summary>
    public void Dispose()
    {
        m_Executor?.Dispose();
    }
}

