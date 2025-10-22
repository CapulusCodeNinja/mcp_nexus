using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Engine.Internal;
using mcp_nexus.Engine.Models;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Unit tests for the DebugSession class.
/// </summary>
public class DebugSessionTests : IDisposable
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;

    /// <summary>
    /// Initializes a new instance of the DebugSessionTests class.
    /// </summary>
    public DebugSessionTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();
        m_LoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        m_Configuration = new DebugEngineConfiguration
        {
            MaxConcurrentSessions = 5,
            DefaultCommandTimeout = TimeSpan.FromSeconds(30),
            SessionInitializationTimeout = TimeSpan.FromMinutes(1)
        };
        
        SetupDefaultMocks();
    }

    /// <summary>
    /// Sets up default mock behaviors to prevent real system access.
    /// </summary>
    private void SetupDefaultMocks()
    {
        // Setup file system mocks - return false for ALL file existence checks to prevent real system access
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);
        
        m_MockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("\\", paths));

        // Setup ALL other file system methods to prevent real system access
        m_MockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
            .Returns("mocked content");
        
        m_MockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();
        
        m_MockFileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
            .Verifiable();
        
        m_MockFileSystem.Setup(fs => fs.GetFileName(It.IsAny<string>()))
            .Returns<string>(path => System.IO.Path.GetFileName(path));
        
        m_MockFileSystem.Setup(fs => fs.GetDirectoryName(It.IsAny<string>()))
            .Returns<string>(path => System.IO.Path.GetDirectoryName(path));

        // Setup process manager mocks - return null to avoid process-related issues in tests
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns((System.Diagnostics.Process)null!);
        
        m_MockProcessManager.Setup(pm => pm.KillProcess(It.IsAny<System.Diagnostics.Process>()))
            .Verifiable();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        m_LoggerFactory?.Dispose();
    }

    /// <summary>
    /// Creates a DebugSession instance for testing.
    /// </summary>
    /// <returns>A new DebugSession instance.</returns>
    private DebugSession CreateDebugSession()
    {
        // Mock the file system to return true for the dump file and CDB executable so initialization can succeed
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);
        
        // Mock CDB executable path
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);
        
        var session = new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        return session;
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var session = CreateDebugSession();

        // Assert
        session.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            null!,
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void Constructor_WithNullDumpFilePath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            null!,
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            null!,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            null!,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Fact]
    public void Constructor_WithNullFileSystem_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            null!,
            m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    [Fact]
    public void Constructor_WithNullProcessManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            null!);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("processManager");
    }

    [Fact]
    public void EnqueueCommand_WithValidCommand_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateDebugSession();
        var command = "lm";

        // Act & Assert
        var action = () => session.EnqueueCommand(command);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session is not active (current state: Initializing)");
    }

    [Fact]
    public void EnqueueCommand_WithNullCommand_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act & Assert
        var action = () => session.EnqueueCommand(null!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session is not active (current state: Initializing)");
    }

    [Fact]
    public void EnqueueCommand_WithEmptyCommand_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act & Assert
        var action = () => session.EnqueueCommand(string.Empty);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session is not active (current state: Initializing)");
    }

    [Fact]
    public void GetCommandInfo_WithValidCommandId_ShouldReturnNull()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var commandInfo = session.GetCommandInfo("some-command-id");

        // Assert
        commandInfo.Should().BeNull();
    }

    [Fact]
    public void GetCommandInfo_WithInvalidCommandId_ShouldReturnNull()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var commandInfo = session.GetCommandInfo("invalid-id");

        // Assert
        commandInfo.Should().BeNull();
    }

    [Fact]
    public void GetAllCommandInfos_ShouldReturnEmptyCollection()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var allCommands = session.GetAllCommandInfos();

        // Assert
        allCommands.Should().BeEmpty();
    }

    [Fact]
    public void CancelCommand_WithValidCommandId_ShouldReturnFalse()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var result = session.CancelCommand("some-command-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_WithInvalidCommandId_ShouldReturnFalse()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var result = session.CancelCommand("invalid-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CancelAllCommands_ShouldReturnZero()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var result = session.CancelAllCommands();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void State_ShouldReturnInitializing()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var state = session.State;

        // Assert
        state.Should().Be(SessionState.Initializing);
    }

    [Fact]
    public void Dispose_WhenCalled_ShouldDisposeSession()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        session.Dispose();

        // Assert
        // Should not throw when disposed
        var action = () => session.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act & Assert
        session.Dispose();
        var action = () => session.Dispose();
        action.Should().NotThrow();
    }
}
