using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using Xunit;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.Models;
using mcp_nexus.Engine.UnitTests.TestHelpers;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Unit tests for the DebugSession class.
/// </summary>
public class DebugSessionTests : IDisposable
{
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly string m_TestSessionId = "sess-test-123";
    private readonly string m_TestDumpPath = @"C:\Test\test.dmp";
    private readonly string m_TestSymbolPath = @"C:\Symbols";

    public DebugSessionTests()
    {
        m_LoggerFactory = NullLoggerFactory.Instance;
        m_Configuration = TestDataBuilder.CreateDebugEngineConfiguration();
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();
        
        // Setup default mocks
        SetupDefaultMocks();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var session = new mcp_nexus.Engine.Internal.DebugSession(
            m_TestSessionId, 
            m_TestDumpPath, 
            m_TestSymbolPath, 
            m_Configuration, 
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Assert
        session.Should().NotBeNull();
        session.SessionId.Should().Be(m_TestSessionId);
        session.State.Should().Be(SessionState.Initializing);
    }

    [Fact]
    public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new mcp_nexus.Engine.Internal.DebugSession(
            null!, 
            m_TestDumpPath, 
            m_TestSymbolPath, 
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
        var action = () => new mcp_nexus.Engine.Internal.DebugSession(
            m_TestSessionId, 
            null!, 
            m_TestSymbolPath, 
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
        var action = () => new mcp_nexus.Engine.Internal.DebugSession(
            m_TestSessionId, 
            m_TestDumpPath, 
            m_TestSymbolPath, 
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
        var action = () => new mcp_nexus.Engine.Internal.DebugSession(
            m_TestSessionId, 
            m_TestDumpPath, 
            m_TestSymbolPath, 
            m_Configuration, 
            null!,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Fact]
    public void IsActive_WhenSessionIsInitializing_ShouldReturnFalse()
    {
        // Arrange
        var session = new mcp_nexus.Engine.Internal.DebugSession(
            m_TestSessionId, 
            m_TestDumpPath, 
            m_TestSymbolPath, 
            m_Configuration, 
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Dispose_WhenCalled_ShouldDisposeSession()
    {
        // Arrange
        var session = new mcp_nexus.Engine.Internal.DebugSession(
            m_TestSessionId, 
            m_TestDumpPath, 
            m_TestSymbolPath, 
            m_Configuration, 
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        session.Dispose();

        // Assert
        var action = () => session.EnqueueCommand("test");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var session = new mcp_nexus.Engine.Internal.DebugSession(
            m_TestSessionId, 
            m_TestDumpPath, 
            m_TestSymbolPath, 
            m_Configuration, 
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () =>
        {
            session.Dispose();
            session.Dispose();
        };
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    private void SetupDefaultMocks()
    {
        // Setup file system mocks
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        
        m_MockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("\\", paths));

        // Setup process manager mocks - create a mock Process that simulates a started process
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns(() => {
                var mockProcess = new Mock<System.Diagnostics.Process>();
                mockProcess.Setup(p => p.StandardInput).Returns(new Mock<System.IO.StreamWriter>().Object);
                mockProcess.Setup(p => p.StandardOutput).Returns(new Mock<System.IO.StreamReader>().Object);
                mockProcess.Setup(p => p.StandardError).Returns(new Mock<System.IO.StreamReader>().Object);
                mockProcess.Setup(p => p.Id).Returns(12345);
                mockProcess.Setup(p => p.HasExited).Returns(false);
                return mockProcess.Object;
            });
    }
}
