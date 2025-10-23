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
            ScriptPath = "test.ps1",
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
    /// Cleanup after each test.
    /// </summary>
    public void Dispose()
    {
        m_Executor?.Dispose();
    }
}

