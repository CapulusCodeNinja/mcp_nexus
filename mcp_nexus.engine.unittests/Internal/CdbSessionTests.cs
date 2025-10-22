using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using Xunit;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.UnitTests.TestHelpers;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Unit tests for the CdbSession class.
/// </summary>
public class CdbSessionTests : IDisposable
{
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private mcp_nexus.Engine.Internal.CdbSession? m_CdbSession;

    public CdbSessionTests()
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
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>();
        m_CdbSession = new mcp_nexus.Engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Assert
        m_CdbSession.Should().NotBeNull();
        m_CdbSession.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>();
        var action = () => new mcp_nexus.Engine.Internal.CdbSession(null!, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new mcp_nexus.Engine.Internal.CdbSession(m_Configuration, null!, m_MockFileSystem.Object, m_MockProcessManager.Object);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void IsActive_WhenNotInitialized_ShouldReturnFalse()
    {
        // Arrange
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>();
        m_CdbSession = new mcp_nexus.Engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        m_CdbSession.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Dispose_WhenCalled_ShouldDisposeSession()
    {
        // Arrange
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>();
        m_CdbSession = new mcp_nexus.Engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        m_CdbSession.Dispose();

        // Assert
        var action = async () => await m_CdbSession.ExecuteCommandAsync("test");
        action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>();
        m_CdbSession = new mcp_nexus.Engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        var action = () =>
        {
            m_CdbSession!.Dispose();
            m_CdbSession.Dispose();
        };
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        m_CdbSession?.Dispose();
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
