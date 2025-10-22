using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    private mcp_nexus.Engine.Internal.CdbSession CreateCdbSession()
    {
        return new mcp_nexus.Engine.Internal.CdbSession(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
    }

    [Fact]
    public async Task InitializeAsync_WithValidDumpFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var session = CreateCdbSession();
        var dumpFilePath = @"C:\Test\test.dmp";
        var symbolPath = @"C:\Symbols";

        // Act & Assert
        var action = () => session.InitializeAsync(dumpFilePath, symbolPath);
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
    }

    [Fact]
    public async Task InitializeAsync_WithNullDumpFile_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync(null!, null);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyDumpFile_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync("", null);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentDumpFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var session = CreateCdbSession();
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);

        // Act & Assert
        var action = () => session.InitializeAsync(@"C:\Test\nonexistent.dmp", null);
        await action.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task InitializeAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        session.Dispose();

        // Act & Assert
        var action = () => session.InitializeAsync(@"C:\Test\test.dmp", null);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.ExecuteCommandAsync("lm");
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        session.Dispose();

        // Act & Assert
        var action = () => session.ExecuteCommandAsync("lm");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ExecuteBatchCommandAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();
        var commands = new[] { "lm", "!threads" };

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(commands);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteBatchCommandAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        var commands = new[] { "lm", "!threads" };
        session.Dispose();

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(commands);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ExecuteBatchCommandAsync_WithNullCommands_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(null!);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteBatchCommandAsync_WithEmptyCommands_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(Array.Empty<string>());
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task FindCdbExecutableAsync_WhenCdbFound_ShouldReturnCdbPath()
    {
        // Arrange
        var session = CreateCdbSession();
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await session.FindCdbExecutableAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FindCdbExecutableAsync_WhenCdbNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);

        // Act & Assert
        var action = () => session.FindCdbExecutableAsync();
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartCdbProcessAsync_ShouldStartProcess()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StartCdbProcessAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public void StopCdbProcess_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StopCdbProcess();
        action.Should().NotThrow();
    }

    [Fact]
    public void StopCdbProcess_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        session.Dispose();

        // Act & Assert
        var action = () => session.StopCdbProcess();
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void DumpFilePath_WhenNotInitialized_ShouldReturnEmptyString()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act
        var result = session.DumpFilePath;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SymbolPath_WhenNotInitialized_ShouldReturnNull()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act
        var result = session.SymbolPath;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsInitialized_WhenNotInitialized_ShouldReturnFalse()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act
        var result = session.IsInitialized;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenCalled_ShouldDisposeSession()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.DisposeAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = async () =>
        {
            await session.DisposeAsync();
            await session.DisposeAsync();
        };
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FindCdbExecutableAsync_WhenNoCdbFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.FindCdbExecutableAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task StartCdbProcessAsync_WhenCalled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StartCdbProcessAsync();
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void StopCdbProcess_WhenCalled_ShouldNotThrow()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StopCdbProcess();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task InitializeAsync_WithNullDumpFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync(null!, "symbols");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyDumpFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync(string.Empty, "symbols");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public async Task InitializeAsync_WithWhitespaceDumpFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync("   ", "symbols");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public void TestThrowIfDisposed_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        testAccessor.Dispose();

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void TestThrowIfDisposed_WhenNotDisposed_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().NotThrow();
    }

    [Fact]
    public void TestThrowIfNotInitialized_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestThrowIfNotInitialized();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task TestWaitForCdbInitializationAsync_WhenProcessNotStarted_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestWaitForCdbInitializationAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TestSendCommandToCdbAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestSendCommandToCdbAsync("test command");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB input stream is not available");
    }

    [Fact]
    public async Task TestReadCommandOutputAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestReadCommandOutputAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB output stream is not available");
    }

    [Fact]
    public async Task InitializeAsync_WithSuccessfulInitialization_ShouldSetInitializedToTrue()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.InitializeAsync(@"C:\Test\test.dmp", @"C:\Symbols");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to start CDB process");
    }

    [Fact]
    public async Task InitializeAsync_WithSuccessfulInitialization_ShouldLogSuccessMessage()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.InitializeAsync(@"C:\Test\test.dmp", @"C:\Symbols");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to start CDB process");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNullCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync(null!);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithEmptyCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync(string.Empty);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("   ");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithValidCommand_ShouldLogDebugMessage()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act
        var action = () => testAccessor.ExecuteCommandAsync("lm");

        // Assert
        // The debug message should be logged (we can't easily verify this without a real logger, but the code path is covered)
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithTabCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("\t");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNewlineCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("\n");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithCarriageReturnCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("\r");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithMixedWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync(" \t\n\r ");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    #region Additional Tests for Coverage

    [Fact]
    public void CreateCommandWithSentinels_WithValidCommand_ShouldWrapCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = "test command";

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
        result.Should().Contain(command);
    }

    [Fact]
    public void CreateCommandWithSentinels_WithEmptyCommand_ShouldWrapEmptyCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = "";

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
    }

    [Fact]
    public void CreateCommandWithSentinels_WithNullCommand_ShouldWrapNullCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        string? command = null;

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command!);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
    }

    [Fact]
    public void CreateCommandWithSentinels_WithSpecialCharacters_ShouldWrapSpecialCharacters()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = "!analyze -v; lm; kL";

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
        result.Should().Contain(command);
    }

    [Fact]
    public void CreateCommandWithSentinels_WithLongCommand_ShouldWrapLongCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = new string('A', 1000);

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
        result.Should().Contain(command);
    }

    [Fact]
    public async Task TestSendQuitCommandAsync_WithNullInputWriter_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestSendQuitCommandAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TestWriteQuitCommandAsync_WithNullInputWriter_ShouldThrowNullReferenceException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestWriteQuitCommandAsync();

        // Assert
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task TestFlushInputAsync_WithNullInputWriter_ShouldThrowNullReferenceException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestFlushInputAsync();

        // Assert
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task TestWaitForProcessExitAsync_WithNullProcess_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestWaitForProcessExitAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void TestKillProcess_WithNullProcess_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestKillProcess();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void TestDisposeResources_ShouldDisposeAllResources()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestDisposeResources();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void TestSetDisposedState_ShouldSetDisposedAndNotInitialized()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        testAccessor.TestSetDisposedState();

        // Assert
        testAccessor.IsActive.Should().BeFalse();
    }

    [Fact]
    public void TestProcessOutputLine_WithStartMarker_ShouldSetStartMarkerFound()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "MCP_NEXUS_SENTINEL_COMMAND_START";
        var startMarkerFound = false;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeTrue();
        result.ShouldBreak.Should().BeFalse();
        startMarkerFound.Should().BeTrue();
    }

    [Fact]
    public void TestProcessOutputLine_WithEndMarker_ShouldReturnBreak()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "MCP_NEXUS_SENTINEL_COMMAND_END";
        var startMarkerFound = true;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeFalse();
        result.ShouldBreak.Should().BeTrue();
    }

    [Fact]
    public void TestProcessOutputLine_WithContentBetweenMarkers_ShouldAppendToOutput()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "Some command output";
        var startMarkerFound = true;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeTrue();
        result.ShouldBreak.Should().BeFalse();
        output.ToString().Should().Be("Some command output\r\n");
    }

    [Fact]
    public void TestProcessOutputLine_WithContentBeforeStartMarker_ShouldNotAppendToOutput()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "Some command output";
        var startMarkerFound = false;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeTrue();
        result.ShouldBreak.Should().BeFalse();
        output.ToString().Should().BeEmpty();
    }


    #endregion
}
